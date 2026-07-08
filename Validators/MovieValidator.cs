using FluentValidation;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Validators
{
    /// <summary>
    /// Validator cho Model Movie
    /// Tệp này HOÀN TOÀN TỰ LẬP khỏi Model Movie (được tự động sinh ra từ DB-First)
    /// để tránh bị ghi đè khi cập nhật database.
    /// 
    /// Lưu ý: Đặt file này ở thư mục riêng biệt Validators/ để rõ ràng về tác dụng
    /// </summary>
    public class MovieValidator : AbstractValidator<Movie>
    {
        public MovieValidator()
        {
            // ========== TITLE VALIDATION ==========
            RuleFor(m => m.Title)
                .NotEmpty()
                    .WithMessage("Tên phim không được để trống")
                .MinimumLength(1)
                    .WithMessage("Tên phim phải có ít nhất 1 ký tự")
                .MaximumLength(500)
                    .WithMessage("Tên phim không được vượt quá 500 ký tự")
                .Matches(@"^[^\<\>]*$")
                    .WithMessage("Tên phim không được chứa ký tự đặc biệt: < >");

            // ========== GENRE VALIDATION ==========
            RuleFor(m => m.Genre)
                .NotEmpty()
                    .WithMessage("Thể loại phim không được để trống")
                .MinimumLength(1)
                    .WithMessage("Thể loại phim phải có ít nhất 1 ký tự")
                .MaximumLength(100)
                    .WithMessage("Thể loại phim không được vượt quá 100 ký tự");

            // ========== DURATION VALIDATION ==========
            RuleFor(m => m.DurationInMinutes)
                .GreaterThan(0)
                    .WithMessage("Thời lượng phim phải lớn hơn 0 phút")
                .LessThanOrEqualTo(720)
                    .WithMessage("Thời lượng phim không được vượt quá 720 phút (12 giờ)");

            // ========== RELEASE DATE VALIDATION ==========
            RuleFor(m => m.ReleaseDate)
                .NotEmpty()
                    .WithMessage("Ngày phát hành không được để trống")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now))
                    .WithMessage("Ngày phát hành không được lớn hơn ngày hôm nay");

            // ========== DESCRIPTION VALIDATION (Optional) ==========
            RuleFor(m => m.Description)
                .MaximumLength(2000)
                    .WithMessage("Mô tả phim không được vượt quá 2000 ký tự")
                .When(m => !string.IsNullOrEmpty(m.Description));

            // ========== POSTER URL VALIDATION (Optional) ==========
            RuleFor(m => m.PosterUrl)
                .Must(BeAValidUrl)
                    .WithMessage("URL poster không hợp lệ")
                .When(m => !string.IsNullOrEmpty(m.PosterUrl));
        }

        /// <summary>
        /// Hàm kiểm tra URL có hợp lệ không
        /// </summary>
        private bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true; // Optional field, cho phép null

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
