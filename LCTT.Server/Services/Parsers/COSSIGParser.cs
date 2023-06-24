namespace LCTT.Server.Services.Parsers;

public class COSSIGParser : Parser
{
    protected override string RulesPath => "Configs/Rules/COSSIG.yml";
    protected override string TemplatePath => "Configs/Templates/COSSIG.md";
    protected override string TopLevelHeading => "#";
}