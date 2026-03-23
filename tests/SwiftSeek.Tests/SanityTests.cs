using SwiftSeek;
using Xunit;

namespace SwiftSeek.Tests
{
    public class SanityTests
    {
        [Fact]
        public void DefaultOptionsHaveExpectedValues()
        {
            var options = new SearchOptions();

            Assert.False(options.SearchContent);
            Assert.False(options.UseRegex);
            Assert.False(options.CaseSensitive);
            Assert.False(options.ExactPhrase);
        }
    }
}
