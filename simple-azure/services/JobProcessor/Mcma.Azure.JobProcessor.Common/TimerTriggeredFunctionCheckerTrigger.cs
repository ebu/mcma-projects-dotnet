using System.Threading.Tasks;

namespace Mcma.Azure.JobProcessor.Common
{
    public class TimerTriggeredFunctionCheckerTrigger : IJobCheckerTrigger
    {
        public const string AppSettingKey = "TIMER_TRIGGER_DISABLED";

        public Task EnableAsync() => FunctionAppSettingsHelper.SetAppSettingAsync(AppSettingKey, false.ToString());

        public Task DisableAsync() => FunctionAppSettingsHelper.SetAppSettingAsync(AppSettingKey, true.ToString());
    }
}