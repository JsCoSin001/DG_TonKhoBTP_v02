// Independent acceptance harness. Does not modify application sources or open the original database.
// Compile/run with UC_BCTonKho.Acceptance.ps1. Each scenario gets its own persisted SQLite fixture.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Database.Kho;
using DG_TonKhoBTP_v02.Printer.Kho;
using DG_TonKhoBTP_v02.UI;
using DG_TonKhoBTP_v02.UI.Helper;

public static class InventoryAcceptance
{
    static string root, evidence, fixture, schema, seed;
    static Assembly assembly = typeof(UC_BCTonKho).Assembly;
    static BindingFlags inst = BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static Type db = assembly.GetType("DG_TonKhoBTP_v02.Database.Kho.TonKho.BC_TonKho_DB",true);
    static Type kind = assembly.GetType("DG_TonKhoBTP_v02.Models.Kho.TonKho.BCTonKho_LoaiTimKiem",true);
    static List<string[]> results = new List<string[]>();
    static List<string> sqlLog = new List<string>();
    static object P(object x,string key) { return x.GetType().GetProperty(key).GetValue(x,null); }
    static long N(object x,string key) { return Convert.ToInt64(P(x,key)); }
    static object F(object x,string key) { return x.GetType().GetField(key,inst).GetValue(x); }
    static object Call(object x,string method,params object[] args) {
        try { return x.GetType().GetMethod(method,inst).Invoke(x,args); }
        catch(TargetInvocationException ex) { throw ex.InnerException; }
    }
    static void Fresh(string id,bool corrupt=false) {
        SQLiteConnection.ClearAllPools();
        fixture=Path.Combine(evidence,"cases",id+".db");
        if(File.Exists(fixture)) File.Delete(fixture);
        var text=schema;
        if(corrupt) text=text.Replace("SoDau INTEGER NOT NULL,SoCuoi INTEGER NOT NULL","SoDau INTEGER,SoCuoi INTEGER");
        DB_Base.SetDatabasePath(fixture);
        Sql("PRAGMA foreign_keys=ON;BEGIN;"+text+"\nALTER TABLE DanhSachMaSP ADD COLUMN Ten_KhongDau TEXT;\n"+seed+"\nCOMMIT;");
    }
    static object Sql(string sql,bool scalar=false) {
        if(fixture==null || !Path.GetFullPath(fixture).StartsWith(Path.Combine(evidence,"cases")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fixture path guard");
        using(var c=new SQLiteConnection("Data Source="+fixture+";Version=3;")) {
            c.Open(); using(var cmd=new SQLiteCommand(sql,c)) { return scalar?cmd.ExecuteScalar():(object)cmd.ExecuteNonQuery(); }
        }
    }
    static string Oracle(string sql) {
        var actual=Convert.ToString(Sql(sql,true),System.Globalization.CultureInfo.InvariantCulture);
        sqlLog.Add("FIXTURE "+Path.GetFileName(fixture)+"\n"+sql+"\n=> "+actual+"\n"); return actual;
    }
    static object Await(Task task,bool pump=false) {
        var timer=Stopwatch.StartNew();
        while(!task.IsCompleted) {
            if(pump) Application.DoEvents();
            if(timer.ElapsedMilliseconds>15000) throw new TimeoutException("Task >15s");
            Thread.Sleep(3);
        }
        task.GetAwaiter().GetResult();
        return task.GetType().GetProperty("Result").GetValue(task,null);
    }
    static object Detail(int mode=2,long? id=null,string name="Dây điện thử",bool all=false,CancellationToken ct=default(CancellationToken)) {
        return Await((Task)db.GetMethod("LayChiTietTonAsync",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{Enum.ToObject(kind,mode),id,name,all,ct}));
    }
    static List<object> Rows(object r) { return ((IEnumerable)P(r,"Dong")).Cast<object>().ToList(); }
    static object Row(long source=201,int mode=2,long? id=null,string name="Dây điện thử",bool all=false) {
        return Rows(Detail(mode,id,name,all)).Single(x=>N(x,"TTCuonDayId")==source&&N(x,"Kieu")==1);
    }
    static object Reel(long id=303,bool all=false) { return Rows(Detail(3,id,"",all)).Single(); }
    static DataTable Suggest(string keyword,bool all=false,CancellationToken ct=default(CancellationToken)) {
        return (DataTable)Await((Task)db.GetMethod("TimGoiYAsync",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{keyword,all,ct}));
    }
    static string Items(string key,bool all=false) { return string.Join(";",Suggest(key,all).AsEnumerable().Select(r=>Convert.ToString(r["GiaTri"]))); }
    static string Hash(string path) { using(var s=File.OpenRead(path)) using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(s)).Replace("-",""); }
    static void Test(string id,string title,string expected,Func<string> action,bool corrupt=false) {
        string actual,status;
        try { Fresh(id,corrupt); actual=action(); status=actual==expected?"PASS":"FAIL"; }
        catch(Exception ex) { actual=ex.GetType().Name+": "+ex.Message; status="FAIL"; File.WriteAllText(Path.Combine(evidence,id+"-exception.txt"),ex.ToString(),Encoding.UTF8); }
        results.Add(new[]{id,title,expected,actual,status});
        Console.WriteLine(status+" "+id+" | expected="+expected+" | actual="+actual);
        File.WriteAllLines(Path.Combine(evidence,"acceptance.tsv"),new[]{"ID\tTitle\tExpected\tActual\tStatus"}.Concat(results.Select(r=>string.Join("\t",r.Select(v=>v.Replace("\t"," ").Replace("\r"," ").Replace("\n"," "))))),Encoding.UTF8);
    }
    static void Eq(object expected,object actual) { if(!object.Equals(Convert.ToString(expected),Convert.ToString(actual))) throw new Exception("expected="+expected+", actual="+actual); }
    static void Pump(int ms) { var sw=Stopwatch.StartNew(); while(sw.ElapsedMilliseconds<ms) { Application.DoEvents(); Thread.Sleep(3); } }
    static Form Host(out UC_BCTonKho uc) {
        uc=new UC_BCTonKho(); var form=new Form { Text="UC_BCTonKho - isolated QA fixture",Width=1400,Height=850 };
        form.Controls.Add(uc); uc.Dock=DockStyle.Fill; form.Show(); Application.DoEvents(); return form;
    }
    static DataRowView Choice(int mode,long? id,string value) {
        var t=new DataTable(); t.Columns.Add("Loai",typeof(int));t.Columns.Add("Id",typeof(long));t.Columns.Add("GiaTri");
        t.Rows.Add(mode,(object)id??DBNull.Value,value);return t.DefaultView[0];
    }
    static string CountWaiting() { return Application.OpenForms.Cast<Form>().Count(f=>f.GetType().Name=="FrmWaiting").ToString(); }
    static System.Windows.Forms.Timer WatchAlerts(List<string> messages) {
        var timer=new System.Windows.Forms.Timer { Interval=10 };
        timer.Tick+=(s,e)=>{ foreach(var f in Application.OpenForms.Cast<Form>().ToArray())
            if(f is FrmWaiting && f.ControlBox) { messages.Add(((FrmWaiting)f).MessageText); f.Close(); } };
        timer.Start();return timer;
    }
    static void MoreHistory(string type="CUON_LE") {
        Sql("INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(64,51,5);"+
            "INSERT INTO LichSuCat(KeHoachCatChiTiet_ID,TonCuonLe_ID,LoaiNguon,ChieuDaiCat,SoDauTruoc,SoCuoiTruoc,SoDauCat,SoCuoiCat,SoDauSau,SoCuoiSau,HeSoChieu,MaQuetThucTe,NguoiThucHien) VALUES(64,301,'"+type+"',5,100,200,100,105,105,200,1,'QA','qa');");
    }
    static void LegacyFixture() {
        Sql(@"BEGIN;
CREATE TABLE TTNhapKho(id INTEGER PRIMARY KEY,TTThanhPham_ID INTEGER,Loai TEXT,LoaiDon TEXT,Kieu INTEGER,KhachHang TEXT,ChieuCaoLo REAL);
CREATE TABLE TTBoSung(id INTEGER PRIMARY KEY,DanhSachMaSP_ID INTEGER,TenChiTiet TEXT,M TEXT);
CREATE TABLE TTXuatKho(id INTEGER PRIMARY KEY,TTCuonDay_ID INTEGER,SoDau INTEGER,SoCuon INTEGER);
ALTER TABLE TTCuonDay ADD COLUMN TongChieuDai REAL;
ALTER TABLE TTCuonDay ADD COLUMN GhiChu TEXT;
INSERT INTO TTNhapKho VALUES(901,101,'Lô','Hàng bán',1,'QA Factory',1.2),(902,102,'Cuộn','Hàng bán',1,'QA Factory',NULL);
INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID,NgayNhapKho) VALUES(901,101,'2026-10-08'),(902,102,'2026-10-09');
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon,SoDau,SoCuoi,TongChieuDai) VALUES(209,901,1,300,0,300,300),(210,902,4,200,NULL,NULL,200);
INSERT INTO TTXuatKho VALUES(1,209,250,0),(2,210,NULL,1);
COMMIT;");
    }
    static string ZipText(string path,string entry) { using(var z=ZipFile.OpenRead(path))using(var reader=new StreamReader(z.GetEntry(entry).Open()))return reader.ReadToEnd(); }
    [STAThread] public static int Main(string[] args) {
        Console.OutputEncoding=Encoding.UTF8;
        root=Path.GetFullPath(args[0]); evidence=Path.Combine(root,"docs","verification","uc-bctonkho-evidence");
        Directory.CreateDirectory(Path.Combine(evidence,"cases"));
        schema=File.ReadAllText(Path.Combine(root,"tests","fixtures","execution-edit","base-schema.sql"));
        seed=File.ReadAllText(Path.Combine(root,"tests","UC_BCTonKho.Acceptance.seed.sql"));
        Application.EnableVisualStyles();
        if(args.Length>1 && args[1]=="interactive") {
            Fresh("interactive");UC_BCTonKho uc;using(var host=Host(out uc)) {
                Call(uc,"RenderTonKho",Detail(2,null,"Dây điện thử",true)); Application.Run(host);
            } return 0;
        }
        Test("01-02","Designer construction, controls and API signatures","True",()=> {
            using(var uc=new UC_BCTonKho()) return (F(uc,"cbxKieuTon") is CheckBox && F(uc,"btnTimKiem") is ComboBox && F(uc,"grvShowBaoCao") is DataGridView && db.GetMethod("TimGoiYAsync",BindingFlags.Static|BindingFlags.NonPublic)!=null).ToString(); });
        Test("02-01","Partial Bin LIKE","E1-265081/5-03-00",()=>Items("265081"));
        Test("02-02","Partial product accent search","Dây điện thử",()=>Items("điện thử"));
        Test("02-03","Non-accent search","Dây điện thử",()=>Items("dien thu"));
        Test("02-04","Partial reel identifier","QA-LE20",()=>Items("LE20"));
        Test("02-05","No match","0",()=>Suggest("DOES-NOT-EXIST").Rows.Count.ToString());
        Test("02-06","Whitespace trim","Dây điện thử",()=>Items("  điện thử  "));
        Test("02-07","Literal percent underscore quote slash","1,1,1,1",()=>string.Join(",",new[]{"%","_","'","\\"}.Select(k=>Suggest(k).AsEnumerable().Count(r=>Convert.ToInt32(r["Loai"])==1))));
        Test("02-08","SQL injection payload does not broaden or write","0,8",()=>Suggest("' OR 1=1 --").Rows.Count+","+Oracle("SELECT COUNT(*) FROM TTCuonDay"));
        Test("02-09","Max 50 suggestions","50",()=> { for(int i=0;i<70;i++)Sql("INSERT INTO TTThanhPham(id,DanhSachSP_ID,MaBin) VALUES("+(1000+i)+",1,'MASS-"+i.ToString("D3")+"'); INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID,NgayNhapKho) VALUES("+(1000+i)+","+(1000+i)+",'2026-10-01'); INSERT INTO TTCuonDay(ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon) VALUES("+(1000+i)+",1,100);"); return Suggest("MASS-").Rows.Count.ToString(); });
        Test("02-10","Duplicate names across product IDs deduplicated","1",()=>Suggest("điện thử").Rows.Count.ToString());
        Test("02-11","Suggestion item contains single field","True",()=>Suggest("QA").AsEnumerable().All(r=>!Convert.ToString(r["GiaTri"]).Contains(" | ")).ToString());
        Test("03-01","Bin keeps exact source rows","201,203,201,201",()=>string.Join(",",Rows(Detail(1,101,"")).Select(x=>N(x,"TTCuonDayId"))));
        Test("03-02","Product name includes both product IDs","101,102,103",()=>string.Join(",",Rows(Detail()).Select(x=>N(x,"TTThanhPhamId")).Distinct().OrderBy(x=>x)));
        Test("03-03","Reel choice exactly one reel","302",()=>N(Reel(302),"TonCuonLeId").ToString());
        Test("03-04","Reload reads latest committed stock","6,5",()=>{var before=N(Row(),"SoLuong");Sql("UPDATE LichSuLayCuon SET SoLuong=3 WHERE id=71");return before+","+N(Row(),"SoLuong");});
        Test("04-01","Imported 10 minus taken 2 minus converted 2","6=6",()=>N(Row(),"SoLuong")+"="+Oracle("WITH taken AS(SELECT SUM(SoLuong) n FROM LichSuLayCuon WHERE TTCuonDay_ID=201),converted AS(SELECT COUNT(*) n FROM TonCuonLe cl WHERE cl.TTCuonDay_ID=201 AND EXISTS(SELECT 1 FROM LichSuCat h WHERE h.TonCuonLe_ID=cl.id AND h.LoaiNguon='CUON_CHAN')) SELECT 10-taken.n-converted.n FROM taken,converted"));
        Test("04-02","Distinct converted reel despite repeated conversion history","6",()=>{MoreHistory("CUON_CHAN");return N(Row(),"SoLuong").ToString();});
        Test("04-03","Multiple import lines and lengths kept separate","6/200,2/400",()=>string.Join(",",Rows(Detail(1,101,"")).Where(x=>N(x,"Kieu")==1).Select(x=>N(x,"SoLuong")+"/"+N(x,"ChieuDaiConLai"))));
        Test("04-04","Reservation leaves physical stock unchanged","6,6",()=>{var n=N(Row(),"SoLuong");Sql("UPDATE KeHoachHangQuyCachCuonChan SET SoLuongCuonCanLay=3 WHERE id=41");return n+","+N(Row(),"SoLuong");});
        Test("05-01","Descending markers 996 to 600","396=396",()=>N(Reel(),"ChieuDaiConLai")+"="+Oracle("SELECT ABS(SoCuoi-SoDau) FROM TonCuonLe WHERE id=303"));
        Test("05-02","Valid descending markers no false alarm","False",()=>P(Reel(),"LaBatThuong").ToString());
        Test("05-03","Direct partial initial length from import","396",()=>N(Reel(),"ChieuDaiBanDau").ToString());
        Test("05-04","Converted partial initial length blank","True",()=> (P(Reel(301),"ChieuDaiBanDau")==null).ToString());
        Test("05-05","Individual generated reel lengths","QA-LE100/100,QA-LE20/20",()=>string.Join(",",Rows(Detail(1,101,"")).Where(x=>N(x,"Kieu")==2).Select(x=>P(x,"MaHienThi")+"/"+N(x,"ChieuDaiConLai"))));
        Test("06-01","Repeated cuts update current marker only once","6,95",()=>{MoreHistory();Sql("UPDATE TonCuonLe SET SoDau=105 WHERE id=301");return N(Row(),"SoLuong")+","+N(Reel(301),"ChieuDaiConLai");});
        Test("06-02","Cut from current start","346",()=>{Sql("UPDATE TonCuonLe SET SoDau=946 WHERE id=303");return N(Reel(),"ChieuDaiConLai").ToString();});
        Test("06-03","Cut from current end","346",()=>{Sql("UPDATE TonCuonLe SET SoCuoi=650 WHERE id=303");return N(Reel(),"ChieuDaiConLai").ToString();});
        Test("06-04","Exhausted partial hidden unless include all","0,0,False",()=>{Sql("DELETE FROM KeHoachCatChiTiet WHERE id=63; UPDATE TonCuonLe SET SoDau=600,TrangThai='HET' WHERE id=303");return Rows(Detail(3,303,"")).Count+","+N(Reel(303,true),"ChieuDaiConLai")+","+P(Reel(303,true),"LaBatThuong");});
        Test("06-05","Marker rebase preserves absolute remaining length","396",()=>{Sql("UPDATE TonCuonLe SET SoDau=1200,SoCuoi=804 WHERE id=303");return N(Reel(),"ChieuDaiConLai").ToString();});
        Test("07-01","FIFO pending 8 allocates A6 B2","6,2",()=>N(Row(),"DatCho")+","+N(Row(202),"DatCho"));
        Test("07-02","FIFO pending 2 allocates A2 B0","2,0",()=>{Sql("UPDATE KeHoachHangQuyCachCuonChan SET SoLuongCuonCanLay=4 WHERE id=41");return N(Row(),"DatCho")+","+N(Row(202),"DatCho");});
        Test("07-03","FIFO stable source ID tie breaker","6,2",()=>{Sql("UPDATE TTNhapKhoTP SET NgayNhapKho='2026-10-01' WHERE id=12");return N(Row(),"DatCho")+","+N(Row(202),"DatCho");});
        Test("07-04","Over-reservation reported","True,6,3",()=>{Sql("UPDATE KeHoachHangQuyCachCuonChan SET SoLuongCuonCanLay=12 WHERE id=41");return P(Row(202),"LaBatThuong")+","+N(Row(),"SoLuong")+","+N(Row(202),"SoLuong");});
        Test("07-05","Cancelled plan not reserved","0",()=>{Sql("UPDATE KeHoach SET TrangThai='HUY'");return N(Row(),"DatCho").ToString();});
        Test("07-06","Unstarted cutting group adds one whole reservation","6,3",()=>{Sql("INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,QuyCachCuonChan_ID) VALUES(54,31,41); INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(65,54,20),(66,54,30)");return N(Row(),"DatCho")+","+N(Row(202),"DatCho");});
        Test("07-07","Reservation API does not persist allocation","True",()=>{var h=Hash(fixture);Detail();return (h==Hash(fixture)).ToString();});
        Test("08-01","Partial 396 physical with 150m reserved","396,150",()=>N(Reel(),"ChieuDaiConLai")+","+N(Reel(),"DatCho"));
        Test("08-02","Executed detail excluded; pending details summed once","40",()=>{Sql("INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES(65,53,40); INSERT INTO LichSuCat(KeHoachCatChiTiet_ID,TonCuonLe_ID,LoaiNguon,ChieuDaiCat,SoDauTruoc,SoCuoiTruoc,SoDauCat,SoCuoiCat,SoDauSau,SoCuoiSau,HeSoChieu,MaQuetThucTe,NguoiThucHien) VALUES(63,303,'CUON_LE',150,1146,600,1146,996,996,600,-1,'QA-DIRECT','qa')");return N(Reel(),"DatCho").ToString();});
        Test("08-03","Partial over-reservation flagged","True,396",()=>{Sql("UPDATE KeHoachCatChiTiet SET ChieuDaiCanCat=500 WHERE id=63");return P(Reel(),"LaBatThuong")+","+N(Reel(),"ChieuDaiConLai");});
        Test("09-01","Valid exhausted whole filtered both dropdown and detail","0,0,1,1",()=>Rows(Detail(1,105,"")).Count+","+Suggest("QA-DEPLETED").Rows.Count+","+Rows(Detail(1,105,"",true)).Count+","+Suggest("QA-DEPLETED",true).Rows.Count);
        Test("09-02","Negative anomaly kept when include all is false","-1,True,1",()=>N(Row(207,1,106,""),"SoLuong")+","+P(Row(207,1,106,""),"LaBatThuong")+","+Suggest("QA-NEGATIVE").Rows.Count);
        Test("10-01","Converted quantity greater than imported flagged","-1,True",()=>{Sql("UPDATE TTCuonDay SET SoCuon=1 WHERE id=201;DELETE FROM LichSuLayCuon WHERE id=71");return N(Row(),"SoLuong")+","+P(Row(),"LaBatThuong");});
        Test("10-02","Missing start marker is visible and flagged","True,True",()=>{Sql("UPDATE TonCuonLe SET SoDau=NULL WHERE id=303");var x=Reel();return P(x,"LaBatThuong")+","+(P(x,"ChieuDaiConLai")==null);},true);
        Test("10-03","ACTIVE zero length anomaly kept","True,0",()=>{Sql("UPDATE TonCuonLe SET SoDau=600 WHERE id=303");return P(Reel(),"LaBatThuong")+","+N(Reel(),"ChieuDaiConLai");});
        Test("10-04","HET positive length anomaly kept","True,396",()=>{Sql("UPDATE TonCuonLe SET TrangThai='HET' WHERE id=303");return P(Reel(),"LaBatThuong")+","+N(Reel(),"ChieuDaiConLai");});
        Test("10-05","Missing current partial row flagged","True",()=>{Sql("DELETE FROM TonCuonLe WHERE id=303");return P(Rows(Detail(1,104,"")).Single(),"LaBatThuong").ToString();});
        Test("10-06","Orphan partial with imported header visible","True",()=>{Sql("UPDATE TonCuonLe SET TTCuonDay_ID=99999 WHERE id=303");return P(Reel(),"LaBatThuong").ToString();});
        Test("10-07","Mismatched source link flagged","True",()=>{Sql("UPDATE TonCuonLe SET TTThanhPham_ID=102 WHERE id=301");return Rows(Detail()).Any(x=>N(x,"TonCuonLeId")==301&&Convert.ToBoolean(P(x,"LaBatThuong"))).ToString();});
        Test("10-08","Orphan partial without any import header remains discoverable","1,1",()=>{Sql("DELETE FROM TTNhapKhoTP WHERE id=14");return Rows(Detail(3,303,"")).Count+","+Suggest("QA-DIRECT").Rows.Count;});
        Test("10-09","Generated partial missing conversion history flagged","True",()=>{Sql("DELETE FROM LichSuCat WHERE id=81");return P(Reel(301),"LaBatThuong").ToString();});
        Test("10-12","Missing conversion history must not silently inflate physical inventory","6,True,True",()=>{Sql("DELETE FROM LichSuCat WHERE id=81");var x=Reel(301);return N(Row(),"SoLuong")+","+P(x,"LaBatThuong")+","+(P(x,"ChieuDaiBanDau")==null);});
        Test("10-13","Direct partial code differing from original Bin flagged","True",()=>{Sql("UPDATE TonCuonLe SET MaCuon='QA-WRONG' WHERE id=303");return P(Reel(),"LaBatThuong").ToString();});
        Test("10-14","Missing end marker visible and flagged","True,True",()=>{Sql("UPDATE TonCuonLe SET SoCuoi=NULL WHERE id=303");var x=Reel();return P(x,"LaBatThuong")+","+(P(x,"ChieuDaiConLai")==null);},true);
        Test("10-10","Source-less reservation flagged at product scope without assigning it to unrelated Bin","True,False",()=>{Sql("INSERT INTO KeHoachHangQuyCachCuonChan(id,KeHoachHang_ID,ChieuDai1Cuon,SoLuongCuonCanLay) VALUES(49,31,999,1)");
            return Rows(Detail()).Any(x=>N(x,"ChieuDaiConLai")==999&&Convert.ToBoolean(P(x,"LaBatThuong")))+","+Rows(Detail(1,101,"")).Any(x=>N(x,"ChieuDaiConLai")==999);});
        UiTests();
        Test("13-03","Excel report API and exporter preserve legacy Lo250m Cuon3","250,3,True",()=>{
            LegacyFixture();var lo=TonKhoExcelReport_DB.LayTonKhoLo();var cuon=TonKhoExcelReport_DB.LayTonKhoCuon();
            var output=Path.Combine(evidence,"legacy-report.xlsx");TonKhoExcelExporter.ExportToPath(lo,cuon,output);
            return lo.Rows[0]["Tổng chiều dài tồn"]+","+cuon.Rows[0]["Số cuộn tồn"]+","+ZipText(output,"xl/workbook.xml").Contains("sheet");});
        Test("13-04","Word customer report API and writer retain lot250m","250,True",()=>{
            LegacyFixture();var report=BaoCaoTonKhoLo_DB.LayBaoCaoTonKhoLo();var value=report.Groups[0].Rows[0].HangBan.LoGroups[0].Lots[0].SoM;
            var output=Path.Combine(evidence,"legacy-report.docx");TonKhoLoReportDocxWriter.Write(report,output);
            return value+","+ZipText(output,"word/document.xml").Contains("250");});
        Test("12-01","Pre-cancelled suggestions task","OperationCanceled",()=>{using(var c=new CancellationTokenSource()){c.Cancel();try{Suggest("QA",false,c.Token);return "Completed";}catch(OperationCanceledException){return "OperationCanceled";}}});
        Test("12-02","Pre-cancelled detail task","OperationCanceled",()=>{using(var c=new CancellationTokenSource()){c.Cancel();try{Detail(2,null,"Dây điện thử",false,c.Token);return "Completed";}catch(OperationCanceledException){return "OperationCanceled";}}});
        Test("14-01","APIs work with enforced read-only connection and unchanged bytes","True",()=>{var hash=Hash(fixture); DB_Base._connStr+="Read Only=True;FailIfMissing=True;";Suggest("QA");Detail();return (hash==Hash(fixture)).ToString();});
        Test("14-02","Non imported and temporary source excluded","0,0",()=>{Sql("UPDATE TTThanhPham SET NhapKho=0 WHERE id=101;UPDATE TTThanhPham SET Temp=1 WHERE id=102");return Rows(Detail(1,101,"")).Count+","+Rows(Detail(1,102,"")).Count;});
        Test("14-03","All required tables and columns on controlled fixture","12",()=>Oracle("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN('TTThanhPham','DanhSachMaSP','TTNhapKhoTP','TTCuonDay','TonCuonLe','LichSuCat','LichSuLayCuon','KeHoach','KeHoachHang','KeHoachHangQuyCachCuonChan','KeHoachCatNhom','KeHoachCatChiTiet')"));
        Test("14-04","Concurrent writer between detail SELECTs keeps a single snapshot","6,396|5,346|1",()=>{
            Sql("UPDATE DanhSachMaSP SET Ten='Dây điện thử' WHERE id=3;PRAGMA journal_mode=WAL");int wrote=0;
            SQLiteConnectionEventHandler opened=(sender,e)=> {
                if(e.EventType!=SQLiteConnectionEventType.Opened) return;
                var c=sender as SQLiteConnection;if(c==null)return;
                c.Trace+=(s,t)=> { if(t.Statement.Contains("SELECT cl.id, cl.SoDau")&&Interlocked.CompareExchange(ref wrote,1,0)==0)
                    Sql("BEGIN;UPDATE LichSuLayCuon SET SoLuong=3 WHERE id=71;UPDATE TonCuonLe SET SoDau=946 WHERE id=303;COMMIT;"); };
            };
            SQLiteConnection.Changed+=opened;
            object first;
            try { first=Detail(2,null,"Dây điện thử",true); }
            finally { SQLiteConnection.Changed-=opened; }
            var a=Rows(first).Single(x=>N(x,"TTCuonDayId")==201&&N(x,"Kieu")==1);
            var partial=Rows(first).FirstOrDefault(x=>N(x,"TonCuonLeId")==303);
            return N(a,"SoLuong")+","+(partial==null?"missing":N(partial,"ChieuDaiConLai").ToString())+"|"+N(Row(),"SoLuong")+","+N(Reel(),"ChieuDaiConLai")+"|"+wrote;
        });
        File.WriteAllLines(Path.Combine(evidence,"independent-sql.txt"),sqlLog,Encoding.UTF8);
        Console.WriteLine("TOTAL "+results.Count+" PASS "+results.Count(r=>r[4]=="PASS")+" FAIL "+results.Count(r=>r[4]=="FAIL"));
        return results.Any(r=>r[4]=="FAIL")?1:0;
    }
    static void UiTests() {
        Test("11-01","Rendered WinForms grid has exact ten columns","Tên sản phẩm|Mã Bin/LOT|Số lượng|Kiểu|Số đầu|Số cuối|Chiều dài ban đầu|Chiều dài còn lại|Đặt chỗ|Lịch sử",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail());var grid=(DataGridView)F(uc,"grvShowBaoCao");return string.Join("|",grid.Columns.Cast<DataGridViewColumn>().Select(c=>c.HeaderText));}});
        Test("11-02","Reservation display, source Tag and Xem button","6 cuộn (dự kiến),True,Xem",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail());var g=(DataGridView)F(uc,"grvShowBaoCao");var r=g.Rows.Cast<DataGridViewRow>().Single(x=>N(x.Tag,"TTCuonDayId")==201&&N(x.Tag,"Kieu")==1);return r.Cells[8].Value+","+(r.Tag!=null)+","+r.Cells[9].FormattedValue;}});
        Test("11-03","History cell event does not crash or change fixture","True",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail());var g=(DataGridView)F(uc,"grvShowBaoCao");var hash=Hash(fixture);g.GetType().GetMethod("OnCellContentClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{new DataGridViewCellEventArgs(9,0)});return (hash==Hash(fixture)).ToString();}});
        Test("10-11","Danger entire row white text reason tooltip and bitmap","177/48/59,White,True",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail(1,106,""));var g=(DataGridView)F(uc,"grvShowBaoCao");var r=g.Rows[0];using(var b=new Bitmap(host.Width,host.Height)){host.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine(evidence,"danger-grid.png"));}return r.DefaultCellStyle.BackColor.R+"/"+r.DefaultCellStyle.BackColor.G+"/"+r.DefaultCellStyle.BackColor.B+","+r.DefaultCellStyle.ForeColor.Name+","+r.Cells.Cast<DataGridViewCell>().All(c=>!string.IsNullOrEmpty(c.ToolTipText));}});
        Test("10-16","Rendered history button participates in danger row background","177/48/59",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail(1,106,""));var g=(DataGridView)F(uc,"grvShowBaoCao");
                using(var b=new Bitmap(g.Width,g.Height)){g.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));var rect=g.GetCellDisplayRectangle(9,0,true);var color=b.GetPixel(rect.X+rect.Width/2,rect.Y+4);return color.R+"/"+color.G+"/"+color.B;}}});
        Test("03-05","Clear selection removes inventory grid","0,True",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail());Call(uc,"OnTonKhoCleared");var g=(DataGridView)F(uc,"grvShowBaoCao");return g.Rows.Count+","+(F(uc,"_tonKhoSelection")==null);}});
        Test("02-12","Helper selecting item preserves single display text","QA-B",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){var helper=F(uc,"_tonKhoSearchHelper");Call(helper,"FireItemSelected",Choice(1,102,"QA-B"));Pump(400);return ((ComboBox)F(uc,"btnTimKiem")).Text;}});
        Test("09-03","Checkbox reloads selected depleted Bin","0,1",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"OnTonKhoItemSelected",Choice(1,105,"QA-DEPLETED"));Pump(250);var g=(DataGridView)F(uc,"grvShowBaoCao");int before=g.Rows.Count;((CheckBox)F(uc,"cbxKieuTon")).Checked=true;Pump(250);return before+","+g.Rows.Count;}});
        Test("09-04","Checkbox refreshes already displayed suggestions","1,0",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){var box=(ComboBox)F(uc,"btnTimKiem");((CheckBox)F(uc,"cbxKieuTon")).Checked=true;box.Text="QA-DEPLETED";typeof(ComboBox).GetMethod("OnTextUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(box,new object[]{EventArgs.Empty});Pump(650);int before=box.Items.Count;((CheckBox)F(uc,"cbxKieuTon")).Checked=false;Pump(650);return before+","+box.Items.Count;}});
        Test("12-03","Rapid selections newest result wins and Waiting closes","QA-DIRECT,0",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"OnTonKhoItemSelected",Choice(1,102,"QA-B"));Call(uc,"OnTonKhoItemSelected",Choice(3,303,"QA-DIRECT"));Pump(600);var g=(DataGridView)F(uc,"grvShowBaoCao");return g.Rows[0].Cells[1].Value+","+CountWaiting();}});
        Test("12-04","Typing debounces without Waiting form","0,True",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){var b=(ComboBox)F(uc,"btnTimKiem");foreach(string s in new[]{"D","Da","Day","Day dien"}){b.Text=s;typeof(ComboBox).GetMethod("OnTextUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{EventArgs.Empty});Pump(25);}Pump(600);return CountWaiting()+","+(b.Items.Count>0);}});
        Test("12-05","Dispose during detail load no surviving Waiting","0",()=>{
            UC_BCTonKho uc;var host=Host(out uc);Call(uc,"OnTonKhoItemSelected",Choice(1,102,"QA-B"));host.Dispose();Pump(600);return CountWaiting();});
        Test("10-15","Exactly one correctly worded abnormal alert per load","Có dữ liệu bất thường.|1|0",()=>{
            var messages=new List<string>();using(var timer=WatchAlerts(messages)) { UC_BCTonKho uc;using(var host=Host(out uc)) {
                Call(uc,"OnTonKhoItemSelected",Choice(1,106,"QA-NEGATIVE"));Pump(500);return string.Join(";",messages)+"|"+messages.Count+"|"+CountWaiting();}}});
        Test("12-06","Unavailable fixture reports one error and closes Waiting","True,1,0",()=>{
            var messages=new List<string>();using(var timer=WatchAlerts(messages)) {UC_BCTonKho uc;using(var host=Host(out uc)) {
                DB_Base._connStr="Data Source="+Path.Combine(evidence,"cases","absent-directory","missing.db")+";Read Only=True;FailIfMissing=True";
                Call(uc,"OnTonKhoItemSelected",Choice(1,102,"QA-B"));Pump(500);return messages.Any(x=>x.StartsWith("Không thể tải tồn kho:"))+","+messages.Count+","+CountWaiting();}}});
        Test("12-07","Empty detail finishes with empty grid and no Waiting","0,0",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"OnTonKhoItemSelected",Choice(1,99999,"QA-NONE"));Pump(300);return ((DataGridView)F(uc,"grvShowBaoCao")).Rows.Count+","+CountWaiting();}});
        Test("12-08","Checkbox change while loading follows latest filter","1,0",()=>{
            SQLiteConnectionEventHandler delay=(sender,e)=>{if(e.EventType==SQLiteConnectionEventType.Opened && Thread.CurrentThread.GetApartmentState()!=ApartmentState.STA) Thread.Sleep(150);};
            SQLiteConnection.Changed+=delay;
            try {UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"OnTonKhoItemSelected",Choice(1,105,"QA-DEPLETED"));((CheckBox)F(uc,"cbxKieuTon")).Checked=true;Pump(700);return ((DataGridView)F(uc,"grvShowBaoCao")).Rows.Count+","+CountWaiting();}}
            finally{SQLiteConnection.Changed-=delay;}});
        Test("13-01","Switch inventory to old report and back clears columns/styles","1,10,False",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){Call(uc,"RenderTonKho",Detail(1,106,""));var t=new DataTable();t.Columns.Add("OldReport");t.Rows.Add("legacy");Call(uc,"ShowResult",t);var g=(DataGridView)F(uc,"grvShowBaoCao");int n=g.Columns.Count;Call(uc,"RenderTonKho",Detail(1,102,""));return n+","+g.Columns.Count+","+(g.Rows[0].DefaultCellStyle.ForeColor==Color.White);}});
        Test("13-02","btnTonDong executes actual query after inventory view","1,MaBin,True",()=>{
            Sql(@"BEGIN;ALTER TABLE TTThanhPham ADD COLUMN KhoiLuongTruoc REAL;ALTER TABLE TTThanhPham ADD COLUMN KhoiLuongSau REAL;
ALTER TABLE TTThanhPham ADD COLUMN ChieuDaiTruoc REAL;ALTER TABLE TTThanhPham ADD COLUMN ChieuDaiSau REAL;
ALTER TABLE TTThanhPham ADD COLUMN KLBanTran REAL;ALTER TABLE TTThanhPham ADD COLUMN GhiChu TEXT;
UPDATE DanhSachMaSP SET Ten='C 1.02',DonVi='M' WHERE id=1;UPDATE TTThanhPham SET ChieuDaiSau=396 WHERE id=101;COMMIT;");
            DatabaseHelper._connStr=DB_Base._connStr;UC_BCTonKho uc;using(var host=Host(out uc)){
                Call(uc,"RenderTonKho",Detail(1,106,""));Call(uc,"btnTonDong_Click",null,EventArgs.Empty);Pump(600);
                var g=(DataGridView)F(uc,"grvShowBaoCao");return g.Rows.Count+","+g.Columns[0].Name+","+g.AutoGenerateColumns;}});
        Test("13-05","btnBCSX inventory branch loads real snapshot and removes inventory columns","True,True,False",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){
                Call(uc,"RenderTonKho",Detail(1,106,""));UseRealSnapshot();((CheckBox)F(uc,"cbxKeoRut")).Checked=true;
                ((DateTimePicker)F(uc,"dtBatDau")).Value=new DateTime(2025,1,1);((DateTimePicker)F(uc,"dtKetThuc")).Value=new DateTime(2026,10,29);
                Call(uc,"btnBCSX_Click",null,EventArgs.Empty);Pump(1200);var g=(DataGridView)F(uc,"grvShowBaoCao");
                return (g.Rows.Count>0)+","+g.AutoGenerateColumns+","+g.Columns.Contains("LichSu");}});
        Test("13-06","btnBCSX date branch loads real snapshot","True,0",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){
                UseRealSnapshot();((CheckBox)F(uc,"cbxKeoRut")).Checked=true;((CheckBox)F(uc,"cbxBaoCaoTon")).Checked=false;
                ((DateTimePicker)F(uc,"dtBatDau")).Value=new DateTime(2025,1,1);((DateTimePicker)F(uc,"dtKetThuc")).Value=new DateTime(2026,10,29);
                Call(uc,"btnBCSX_Click",null,EventArgs.Empty);Pump(1200);return (((DataGridView)F(uc,"grvShowBaoCao")).Rows.Count>0)+","+CountWaiting();}});
        Test("11-04","Real sample renders 6 whole and 100m20m partial rows","6,100,20",()=>{
            UC_BCTonKho uc;using(var host=Host(out uc)){UseRealSnapshot();var result=Detail(1,57956,"E1-265081/5-03-00",true);Call(uc,"RenderTonKho",result);
                using(var b=new Bitmap(host.Width,host.Height)){host.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine(evidence,"real-sample-grid.png"));}
                var g=(DataGridView)F(uc,"grvShowBaoCao");return g.Rows[0].Cells[2].Value+","+g.Rows[1].Cells[7].Value+","+g.Rows[2].Cells[7].Value;}});
    }
    static void UseRealSnapshot() {
        var path=Path.Combine(evidence,"configured-snapshot.db");if(!File.Exists(path))throw new FileNotFoundException("Configured snapshot required",path);
        DB_Base._connStr="Data Source="+path+";Read Only=True;FailIfMissing=True;Version=3;";DatabaseHelper._connStr=DB_Base._connStr;
    }
}
