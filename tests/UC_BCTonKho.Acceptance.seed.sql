-- Synthetic data only. IDs and schema follow the existing execution-edit fixture.
INSERT INTO users(user_id,username,name) VALUES(1,'qa','QA');
INSERT INTO DanhSachMaSP(id,Ten,Ten_KhongDau,Ma) VALUES
(1,'Dây điện thử','Day dien thu','QA-P1'),(2,'Dây điện thử','Day dien thu','QA-P2'),
(3,'Dây lẻ trực tiếp','Day le truc tiep','QA-P3'),(4,'Hết hợp lệ','Het hop le','QA-P4'),
(5,'Âm bất thường','Am bat thuong','QA-P5'),(6,'Đặc biệt % _ '' \\','Dac biet % _ '' \\','QA-P6');
INSERT INTO TTThanhPham(id,DanhSachSP_ID,MaBin) VALUES
(101,1,'E1-265081/5-03-00'),(102,1,'QA-B'),(103,2,'QA-C'),
(104,3,'QA-DIRECT'),(105,4,'QA-DEPLETED'),(106,5,'QA-NEGATIVE'),(107,6,'QA-%_''\\');
INSERT INTO TTNhapKhoTP(id,TTThanhPham_ID,NgayNhapKho) VALUES
(11,101,'2026-10-01'),(12,102,'2026-10-02'),(13,103,'2026-10-03'),
(14,104,'2026-10-04'),(15,105,'2026-10-05'),(16,106,'2026-10-06'),(17,107,'2026-10-07');
INSERT INTO TTCuonDay(id,ThongTinNhapKho_ID,SoCuon,ChieuDai_1cuon,SoDau,SoCuoi) VALUES
(201,11,10,200,NULL,NULL),(202,12,3,200,NULL,NULL),(203,11,2,400,NULL,NULL),
(204,13,1,200,NULL,NULL),(205,14,1,396,996,600),
(206,15,1,100,NULL,NULL),(207,16,1,100,NULL,NULL),(208,17,1,100,NULL,NULL);
INSERT INTO KeHoach(id,MaKeHoach,Users_id) VALUES(1,'QA-PLAN',1);
INSERT INTO KeHoachHang(id,KeHoach_ID,DanhSachMaSP_ID) VALUES(31,1,1),(32,1,3),(33,1,4),(34,1,5);
INSERT INTO KeHoachHangQuyCachCuonChan(id,KeHoachHang_ID,ChieuDai1Cuon,SoLuongCuonCanLay) VALUES
(41,31,200,10),(42,31,400,0),(43,33,100,1),(44,34,100,2);
INSERT INTO LichSuLayCuon(id,KeHoachHang_ID,TTCuonDay_ID,QuyCachCuonChan_ID,SoLuong,NguoiThucHien) VALUES
(71,31,201,41,2,'qa'),(72,33,206,43,1,'qa'),(73,34,207,44,2,'qa');
INSERT INTO TonCuonLe(id,MaCuon,TTThanhPham_ID,TTCuonDay_ID,SoDau,SoCuoi,TrangThai) VALUES
(301,'QA-LE100',101,201,100,200,'ACTIVE'),(302,'QA-LE20',101,201,180,200,'ACTIVE'),
(303,'QA-DIRECT',104,205,996,600,'ACTIVE');
-- Groups 51/52 are converted whole reels; 53 is an existing partial reel.
INSERT INTO KeHoachCatNhom(id,KeHoachHang_ID,TonCuonLe_ID,QuyCachCuonChan_ID) VALUES
(51,31,301,41),(52,31,302,41),(53,32,303,NULL);
INSERT INTO KeHoachCatChiTiet(id,KeHoachCatNhom_ID,ChieuDaiCanCat) VALUES
(61,51,100),(62,52,180),(63,53,150);
INSERT INTO LichSuCat(id,KeHoachCatChiTiet_ID,TonCuonLe_ID,LoaiNguon,ChieuDaiCat,SoDauTruoc,SoCuoiTruoc,SoDauCat,SoCuoiCat,SoDauSau,SoCuoiSau,HeSoChieu,MaQuetThucTe,NguoiThucHien) VALUES
(81,61,301,'CUON_CHAN',100,0,200,0,100,100,200,1,'QA-WHOLE-1','qa'),
(82,62,302,'CUON_CHAN',180,0,200,0,180,180,200,1,'QA-WHOLE-2','qa');
