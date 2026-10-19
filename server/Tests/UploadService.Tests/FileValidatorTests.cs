using FluentAssertions;
using UploadService.Validation;

namespace UploadService.Tests;

public class FileValidatorTests
{
    [Fact]
    public void IsValidExtension_WhenFileIsPdf_ShouldReturnTrue()
    {
        var validator = new FileValidator();
        var validFileName = "filename.pdf";

        var result = validator.IsValidExtension(validFileName);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidExtension_WhenFileIsPng_ShouldReturnFalse()
    {
        var validator = new FileValidator();
        var validFileName = "filename.png";

        var result = validator.IsValidExtension(validFileName);

        result.Should().BeFalse();
    }
}