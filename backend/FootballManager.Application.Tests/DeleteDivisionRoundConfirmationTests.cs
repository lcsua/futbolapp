using FootballManager.Application.UseCases.Matches.DeleteDivisionRound;

namespace FootballManager.Application.Tests;

public class DeleteDivisionRoundConfirmationTests
{
    [Fact]
    public void Accepts_the_exact_word()
    {
        Assert.True(DeleteDivisionRoundConfirmation.IsValid("ELIMINAR"));
        Assert.True(DeleteDivisionRoundConfirmation.IsValid("  ELIMINAR  "));
    }

    [Fact]
    public void Rejects_anything_else()
    {
        Assert.False(DeleteDivisionRoundConfirmation.IsValid("eliminar"));
        Assert.False(DeleteDivisionRoundConfirmation.IsValid("BORRAR"));
        Assert.False(DeleteDivisionRoundConfirmation.IsValid(null));
        Assert.False(DeleteDivisionRoundConfirmation.IsValid(""));
    }
}
