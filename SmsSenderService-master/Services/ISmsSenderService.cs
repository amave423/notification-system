namespace SmsSenderService.Services
{
    public interface ISmsSenderService { Task<Models.SmsResponse> SendAsync(Models.SmsMessage message); }
}
