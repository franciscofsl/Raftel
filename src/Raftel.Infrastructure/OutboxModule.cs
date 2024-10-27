using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Raftel.Infrastructure.BackgroundJobs;
using Raftel.Shared.Modules;

namespace Raftel.Infrastructure;

public sealed class OutboxModule : RaftelModule
{
    public override void ConfigureCustomServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(configurator =>
        {
            var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

            configurator
                .AddJob<ProcessOutboxMessagesJob>(jobKey)
                .AddTrigger(trigger =>
                    trigger
                        .ForJob(jobKey)
                        .WithSimpleSchedule(schedule =>
                            schedule
                                .WithIntervalInSeconds(100000)
                                .RepeatForever()));
        });

        services.AddQuartzHostedService();
        services.AddTransient<OutboxMessageStore>();
        services.AddTransient<ProcessOutboxMessagesJob>();
    }
}