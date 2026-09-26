using FootballManager.Api.Services;
using FootballManager.Application.Exceptions;

namespace FootballManager.Application.Tests;

public class MatchProcessorCsvTests
{
    [Fact]
    public void ParseRows_ReadsFencedJsonAndSkipsIncompleteRows()
    {
        const string raw = """
            ```json
            {"rows":[
              {"fecha":7,"division":"A","equipo1":"POTERO FC","goles1":5,"equipo2":"DEFENSORES","goles2":0,"estado":"Finalizado"},
              {"fecha":7,"division":"A","equipo1":"","goles1":null,"equipo2":"","goles2":null,"estado":"Finalizado"},
              {"fecha":7,"division":"B","equipo1":"LIBRE FC","goles1":1,"equipo2":"RIVAL","goles2":0,"estado":"Libre"},
              {"fecha":7,"division":"C","equipo1":"SUSPENDIDO FC","equipo2":"OTRO","estado":"Partido Suspendido"},
              {"fecha":7,"division":"C","equipo1":"MAL","goles1":-1,"equipo2":"PEOR","goles2":2,"estado":"Finalizado"}
            ]}
            ```
            """;

        var rows = MatchProcessorService.ParseRows(raw);

        Assert.Equal(3, rows.Count);
        Assert.Equal("Finalizado", rows[0].Estado);
        Assert.Equal(5, rows[0].Goles1);
        Assert.Equal("Libre", rows[1].Estado);
        Assert.Equal("Partido Suspendido", rows[2].Estado);
        Assert.Null(rows[2].Goles1);
    }

    [Fact]
    public void BuildCsv_UsesImporterHeaderAndQuotesCommas()
    {
        var csv = MatchProcessorService.BuildCsv(new[]
        {
            new MatchProcessorService.SheetRow
            {
                Fecha = 9,
                Division = "SENIOR",
                Equipo1 = "ARCO IRIS, SENIOR",
                Goles1 = 2,
                Equipo2 = "BELGRANO OESTE",
                Goles2 = 0,
                Estado = "Finalizado",
            },
        });

        Assert.StartsWith("fecha,division,Equipo 1,goles equipo 1,equipo 2,goles equipo 2,estado\n", csv);
        Assert.Contains("\n9,SENIOR,\"ARCO IRIS, SENIOR\",2,BELGRANO OESTE,0,Finalizado", csv);
    }

    [Theory]
    [InlineData("suspendido", "LOCAL", "VISITA", null, null, "Partido Suspendido")]
    [InlineData("Finalizado", "LOCAL", "VISITA", 1, 0, "Finalizado")]
    [InlineData("Libre", "LOCAL", "", null, null, "Libre")]
    public void NormalizeEstado_MapsSheetNotes(
        string raw,
        string equipo1,
        string equipo2,
        int? goles1,
        int? goles2,
        string expected)
    {
        var estado = MatchProcessorService.NormalizeEstado(raw, equipo1, equipo2, goles1, goles2);
        Assert.Equal(expected, estado);
    }

    [Fact]
    public void ParseRows_RejectsTextThatIsNotJson()
    {
        var ex = Assert.Throws<BusinessException>(() => MatchProcessorService.ParseRows("no es una planilla"));
        Assert.Contains("no devolvió", ex.Message);
    }
}
