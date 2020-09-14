using System;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using NCrontab;

namespace Mcma.Azure.JobProcessor.Common
{
    public class ConfigurableTimerSchedule : TimerSchedule
    {
        private const string CronScheduleKey = "TIMER_TRIGGER_CRON_SCHEDULE";
        
        public ConfigurableTimerSchedule() => CronSchedule = new CronSchedule(CrontabSchedule.Parse(Environment.GetEnvironmentVariable(CronScheduleKey)));

        private CronSchedule CronSchedule { get; }

        public override DateTime GetNextOccurrence(DateTime now) => CronSchedule.GetNextOccurrence(now);
    }
}