using System.Collections.Generic;

namespace QuanLiSanCauLong.ViewModels
{
    public class ReviewSectionViewModel
    {
        public int FacilityId { get; set; }

        public double AverageRating { get; set; }
        public int TotalCount { get; set; }
        public int FiveStars { get; set; }
        public int FourStars { get; set; }
        public int ThreeStars { get; set; }
        public int TwoStars { get; set; }
        public int OneStar { get; set; }
        public int WithImages { get; set; }
        public int Verified { get; set; }


        public double AvgCleanliness { get; set; }
        public double AvgCourtQuality { get; set; }
        public double AvgService { get; set; }
        public double AvgValue { get; set; }

        public List<ReviewCardViewModel> Reviews { get; set; } = new();
        public int LoadedCount { get; set; }
        public bool HasMore { get; set; }

        public bool CanWriteReview { get; set; }
        public ReviewCardViewModel? UserExistingReview { get; set; }

        public List<BookedCourtItem> BookedCourts { get; set; } = new();

        // FIX: khởi tạo sẵn để tránh null — không cần await
        public HashSet<int> MyLikedReviewIds { get; set; } = new HashSet<int>();
    }

    public class BookedCourtItem
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public DateTime BookingDate { get; set; }
    }
}
