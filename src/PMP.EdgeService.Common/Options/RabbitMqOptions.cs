namespace PMP.EdgeService.Common.Options
{
    public class RabbitMqOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string ExchangeName { get; set; }
    }
}