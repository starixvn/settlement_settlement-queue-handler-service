using Fanex.MailDeliver.MailClient.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RazorEngineCore;
using Settlement.Consumer.Constants;
using Settlement.Consumer.Models;
using System.Collections.Concurrent;


namespace Settlement.Consumer.Services;
public interface SendMailService
{
    void SendMail(NotificationModel item, bool isVoidBetEmail);
}

public class SendMailServiceImp(
    IConfiguration configuration,
    IWebHostEnvironment environment)
    : SendMailService
{
    private static readonly ConcurrentDictionary<int, IRazorEngineCompiledTemplate> CompiledTemplateCache = new();

    public void SendMail(NotificationModel item, bool isVoidBetEmail)
    {
        var mailRecord = new MailRecord
        {
            MailTo = MailTo(item.SportType),
            Title = "NAP - Settlement System",
            Subject = Subject(item),
            Body = MailBody(item, isVoidBetEmail)
        };

        using var mailClient = new Fanex.MailDeliver.MailClient.API.MailDeliver();

        mailClient.SendMail(mailRecord);
    }

    private static string Subject(NotificationModel item)
    {
        return $"Settlement issue - [{item.Action}] - [{item.MatchCode}/{item.MatchId}] {item.HomeName} - {item.AwayName}";
    }

    private string? MailTo(int sportType)
    {
        var emailGroup = sportType switch
        {
            SportType.Soccer => "VoidBet",
            SportType.BasketBall => "VoidBet",
            _ => "OtherSport"
        };

        return configuration.GetSection($"EmailGroups:{emailGroup}").Value;
    }

    private string MailBody(NotificationModel model, bool isVoidBetEmail)
    {
        var templateFileName = isVoidBetEmail ? "void-bet-email.html" : "market-email.html";

        var templatePath = Path.Combine(environment.WebRootPath, "email-templates", templateFileName);
        var template = File.ReadAllText(templatePath);

        return RenderTemplate(template, model);
    }

    private string RenderTemplate(string template, NotificationModel model)
    {
        var hashCode = template.GetHashCode();
        var compiledTemplate = CompiledTemplateCache.GetOrAdd(hashCode, _ =>
        {
            var razorEngine = new RazorEngine();
            return razorEngine.Compile(template);
        });

        return compiledTemplate.Run(model);
    }
}
