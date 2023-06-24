namespace LCTT.Server.Services.Parsers;

public class LCTTParser : Parser
{
    protected override string RulesPath => "Configs/Rules/LCTT.yml";
    protected override string TemplatePath => "Configs/Templates/LCTT.md";
    protected override string CounterPath => "Configs/Counters/Counter.conf";
    protected override string TopLevelHeading => "##";
}