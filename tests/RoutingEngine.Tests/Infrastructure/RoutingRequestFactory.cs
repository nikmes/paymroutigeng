namespace RoutingEngine.Tests.Infrastructure;

internal static class RoutingRequestFactory
{
    public static RoutingRequestDto Create(
        string direction,
        string currency,
        Action<CounterpartyBuilder>? counterparty = null,
        Action<CustomerBuilder>? customer = null)
    {
        var counterpartyBuilder = new CounterpartyBuilder();
        counterparty?.Invoke(counterpartyBuilder);

        var customerBuilder = new CustomerBuilder();
        customer?.Invoke(customerBuilder);

        return new RoutingRequestDto(
            new PaymentDto(direction, currency),
            counterpartyBuilder.Build(),
            customerBuilder.Build());
    }

    internal sealed class CounterpartyBuilder
    {
        private string? _bankCountryCode;
        private string? _bankBic;
        private string? _account;
        private string? _name;
        private string? _type;

        public CounterpartyBuilder WithBankCountryCode(string value)
        {
            _bankCountryCode = value;
            return this;
        }

        public CounterpartyBuilder WithBankBic(string value)
        {
            _bankBic = value;
            return this;
        }

        public CounterpartyBuilder WithAccount(string value)
        {
            _account = value;
            return this;
        }

        public CounterpartyBuilder WithName(string value)
        {
            _name = value;
            return this;
        }

        public CounterpartyBuilder WithType(string value)
        {
            _type = value;
            return this;
        }

        public CounterpartyDto Build() => new(_bankCountryCode, _bankBic, _account, _name, _type);
    }

    internal sealed class CustomerBuilder
    {
        private string? _id;
        private string? _industry;
        private string? _type;
        private string? _account;

        public CustomerBuilder WithId(string value)
        {
            _id = value;
            return this;
        }

        public CustomerBuilder WithIndustry(string value)
        {
            _industry = value;
            return this;
        }

        public CustomerBuilder WithType(string value)
        {
            _type = value;
            return this;
        }

        public CustomerBuilder WithAccount(string value)
        {
            _account = value;
            return this;
        }

        public CustomerDto Build() => new(_id, _industry, _type, _account);
    }
}
