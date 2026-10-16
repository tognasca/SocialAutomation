namespace SocialAutomation.Application.CRM.Services;

public static class LeadScoreCalculator
{
    public static string ClassifyScore(int score)
    {
        return score switch
        {
            < 30 => "Frio",
            >= 30 and < 60 => "Morno",
            >= 60 and < 90 => "Quente",
            _ => "Alta Intenção"
        };
    }
}