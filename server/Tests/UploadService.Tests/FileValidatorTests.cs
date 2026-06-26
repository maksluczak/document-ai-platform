namespace UploadService.Tests;

using Xunit;
using FluentAssertions;
using UploadService.Validation;

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