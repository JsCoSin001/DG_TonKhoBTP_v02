#nullable enable

using System;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
    /// <summary>
    /// Trạng thái draft đang được nạp trên form.
    /// Draft được nhận diện riêng, không dùng EditModel/KieuXuLy = 2.
    /// </summary>
    public sealed class DraftContext
    {
        public long DraftId { get; private set; }
        public string DraftMaBin { get; private set; } = string.Empty;

        public bool IsDraftLoaded =>
            DraftId > 0 && !string.IsNullOrWhiteSpace(DraftMaBin);

        public void Set(long draftId, string maBin)
        {
            if (draftId <= 0)
                throw new ArgumentOutOfRangeException(nameof(draftId));

            if (string.IsNullOrWhiteSpace(maBin))
                throw new ArgumentException("MaBin của draft không hợp lệ.", nameof(maBin));

            DraftId = draftId;
            DraftMaBin = maBin.Trim();
        }

        public void Clear()
        {
            DraftId = 0;
            DraftMaBin = string.Empty;
        }
    }
}
