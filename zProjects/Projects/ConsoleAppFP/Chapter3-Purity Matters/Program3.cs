using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace ConsoleAppFP.Chapter3_Purity_Matters
{
    internal class Program3
    {

    }

    public abstract record Command(DateTime Timestamp);

    public record MakeTransfer(Guid DebitedAccountId, string Beneficiary, string Iban, string Bic, DateTime Date, decimal Amount, string Reference, DateTime TimeStamp = default) : Command(TimeStamp)
    {
        public static MakeTransfer Dummy => new(default, default!, default!, default!, default!, default!, default!);
    }

    public interface IDateTimeService
    {
        DateTime UtcNow { get; }
    }

    public class DefaultDateTimeService : IDateTimeService
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }


    public interface IValidator<T>
    {
        bool IsValid(T t);
    }

    public class BicFormatValidator : IValidator<MakeTransfer>
    {
        static readonly Regex regex = new Regex("^[A-Z]{6}[A-Z1-9]{5}$");
        public bool IsValid(MakeTransfer transfer)
        => regex.IsMatch(transfer.Bic);
    }

    public class DateNotPastValidator : IValidator<MakeTransfer>
    {
        private readonly IDateTimeService dateService;
        public DateNotPastValidator(IDateTimeService dateService)
        {
            this.dateService = dateService;
        }
        public bool IsValid(MakeTransfer transfer) => dateService.UtcNow.Date <= transfer.Date.Date;
    }

    public record DateNotPastValidatorPure(DateTime Today) : IValidator<MakeTransfer>
    {
        public bool IsValid(MakeTransfer transfer) => Today <= transfer.Date.Date;  
    }

    public record DateNotPastValidatorPureWithFunc(Func<DateTime> Clock) : IValidator<MakeTransfer>
    {
        public bool IsValid(MakeTransfer transfer) => Clock().Date <= transfer.Date.Date;
    }

    public class DateNotPastValidatorTest
    {
        static DateTime presentDate = new DateTime(2021, 3, 12);

        private class FakeDateTimeService : IDateTimeService
        {
            public DateTime UtcNow => presentDate;
        }

        public void WhenTransferDateIsPast_ThenValidationFails()
        {
            var svc = new FakeDateTimeService();
            var sut = new DateNotPastValidator(svc);

            var transfer = MakeTransfer.Dummy with
            {
                Date = presentDate.AddDays(-1)
            };

            // Assert.AreEqual(false, sut.IsValid(transfer));
        }
    }
}