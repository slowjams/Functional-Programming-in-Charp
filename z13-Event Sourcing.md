Chapter 13-Event Sourcing
=========================================

If you've ever done any parallel programming, you probably used some lock techniques. There is some performance hit when you use `Lock` or `Monitor`. For example, let's take a bank account balance for example, if there are two tranctions are processed by the bank concurrently for a client and the application or DB engine doesn't use any locking techniques, the client might lose money.

But if we don't compute the balance immediately and just save tranctions data (events) first, transactionOne: +100, transactionTwo: -30, then when the client want to see the balance by clicking a button, the bank's application reply those "event" on the original balance, then there is no locks ever needed in the application which increases the performance.

Let's define `Event` first:

```C#
public abstract rec൦rd Event(Guid EntityId, DateTime Timestamp); 

public rec൦rd CreatedAccount
(
   Guid EntityId, 
   DateTime Timestamp, 
   CurrencyCode Currency
): Event(EntityId, Timestamp);

public rec൦rd FrozeAccount
(
   Guid EntityId,
   DateTime Timestamp
)
: Event(EntityId, Timestamp);

public rec൦rd DepositedCash
(
   Guid EntityId,
   DateTime Timestamp,
   decimal Amount,
   Guid BranchId
)
: Event(EntityId, Timestamp);

public rec൦rd DebitedTransfer
(
   Guid EntityId,
   DateTime Timestamp,
   string Beneficiary,
   string Iban,
   string Bic,
   decimal DebitedAmount,
   string Reference
 )
 : Event(EntityId, Timestamp);

public rec൦rd AlteredOverdraft
(
   Guid EntityId,
   DateTime Timestamp,
   decimal By
)
: Event(EntityId, Timestamp);
```

Those events can be stored in database as (Data column will store json payload):

| EntityId | Timestamp        | EventType       | Data
| -------- | -----------------| --------------  | ----------------------
| 1000     | 2021-07-22 12:40 | CreatedAccount  |  { "Currency": "EUR" }
| 1001     | 2021-07-30 13:25 | DepositedCash   |  { "Amount": 500, "BranchId": "BOCLHAYMCKT" }
| 1002     | 2021-08-03 10:33 | DebitedTransfer |  { "DebitedAmount": 300, "Beneficiary": "Rose Stephens", ...}

then we define commands:

```C#
public abstract rec൦rd Command(DateTime Timestamp);   // command uses normal tense, not sure why the author didn't put AccountId here
 
public rec൦rd CreateAccount
(
   DateTime Timestamp,
   Guid AccountId,
   CurrencyCode Currency
) : Command(Timestamp)
{
   public CreatedAccount ToEvent() => new CreatedAccount  // event uses past tense XXXedXXX
   (
      EntityId: this.AccountId,
      Timestamp: this.Timestamp,
      Currency: this.Currency
   );
}

public rec൦rd FreezeAccount(DateTime Timestamp, Guid AccountId) : Command(Timestamp)
{
   public FrozeAccount ToEvent()
   {
      return new FrozeAccount(EntityId: this.AccountId, Timestamp: this.Timestamp);
   }
}

public rec൦rd MakeTransfer(Guid DebitedAccountId, string Beneficiary, string Iban, string Bic,
                           DateTime Date, decimal Amount, string Reference, DateTime Timestamp = default) : Command(Timestamp)
{
   // useful for testing, when you don't need all the properties to be populated
   internal static MakeTransfer Dummy
      => new(default, default!, default!, default!, default!, default!, default!);

   public DebitedTransfer ToEvent() => new
   (
      Beneficiary: this.Beneficiary,
      Bic: this.Bic,
      DebitedAmount: this.Amount,
      EntityId: this.DebitedAccountId,
      Iban: this.Iban,
      Reference: this.Reference,
      Timestamp: this.Timestamp
   );
}

public rec൦rd AcknowledgeCashDeposit
(
   DateTime Timestamp,
   Guid AccountId,
   decimal Amount,
   Guid BranchId
) : Command(Timestamp)
{
   public DepositedCash ToEvent() => new DepositedCash
   (
      EntityId: this.AccountId,
      Timestamp: this.Timestamp,
      Amount: this.Amount,
      BranchId: this.BranchId
   );
}

public rec൦rd SetOverdraft
(
   DateTime Timestamp,
   Guid AccountId,
   decimal Amount
) : Command(Timestamp)
{
   public AlteredOverdraft ToEvent(decimal by) => new AlteredOverdraft
   (
      EntityId: this.AccountId,
      Timestamp: this.Timestamp,
      By: by
   );
}
```
Note that each command can be converted back to a counterpart event by calling `ToEvent(...)`, you might ask why we need to do this since they look identical. The reason is, Commands can contain some extra data for validation purpose, and we don't need those extra data for events


We also need to populate result for clients from the event history of a given account. Notice that we need the full history of events

```C#
public rec൦rd AccountStatement(int Month, int Year, decimal StartingBalance, decimal EndBalance, IEnumerable<Transaction> Transactions)
{
    public static AccountStatement Create(int month, int year, IEnumerable<Event> events)
    {
        var startOfPeriod = new DateTime(year, month, 1);
        var endOfPeriod = startOfPeriod.AddMonths(1);
        var (eventsBeforePeriod, eventsDuringPeriod) = events.TakeWhile(e => e.Timestamp < endOfPeriod).Partition(e => e.Timestamp <= startOfPeriod);

        decimal startingBalance = eventsBeforePeriod.Aggregate(0m, BalanceReducer);
        decimal endBalance = eventsDuringPeriod.Aggregate(startingBalance, BalanceReducer);

        return new
        (
           Month: month,
           Year: year,
           StartingBalance: startingBalance,
           EndBalance: endBalance,
           Transactions: eventsDuringPeriod.Bind(CreateTransaction)
        );
    }

    public static decimal BalanceReducer(decimal bal, Event evt)
    {
        return evt switch
        {
            DepositedCash e => bal + e.Amount,
            DebitedTransfer e => bal - e.DebitedAmount,
            _ => bal
        };
    }

    public static Option<Transaction> CreateTransaction(Event evt)
    {
        return evt switch
        {
            DepositedCash e => new Transaction(CreditedAmount: e.Amount, Description: $"Deposit at {e.BranchId}", Date: e.Timestamp.Date),
            DebitedTransfer e => new Transaction(DebitedAmount: e.DebitedAmount, Description: $"Transfer to {e.Bic}/{e.Iban}; Ref: {e.Reference}", Date: e.Timestamp.Date),
            _ => None
        };
    }
}

public record Transaction(DateTime Date, string Description, decimal DebitedAmount = 0m, decimal CreditedAmount = 0m);

//------------------------->>
public static class Helpers
{
    public static (IEnumerable<T> Passed, IEnumerable<T> Failed) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var grouped = source.GroupBy(predicate);  // two groupings, each contains a key either true or false
        return
        (
           Passed: grouped.Where(g => g.Key).FirstOrDefault(Enumerable.Empty<T>()),
           Failed: grouped.Where(g => !g.Key).FirstOrDefault(Enumerable.Empty<T>())
        );
    }
}
//-------------------------<<
```

```C#
public static class Account
{
    public delegate Validation<T> Validator<T>(T t);  // Transition is actually a Validator

    public delegate Validation<(T, St)> Transition_<St, T>(St state);  // easy to look to be referenced purpose only   

    //------------------------------------------------------------------------------V
    public static Transition<AccountState, CreatedAccount> Create(CreateAccount cmd)
    {
        return _ =>  // use discard here because CreateAccount is special as it doesn't have prior state
        {
            CreatedAccount evt = cmd.ToEvent();
            AccountState newState = evt.ToAccount();
            return (evt, newState);
        };
    }

    public static Transition<AccountState, DepositedCash> Deposit(AcknowledgeCashDeposit cmd)
    {
        return accountSt =>
        {
            if (accountSt.Status != AccountStatus.Active)
                return Errors.AccountNotActive;

            DepositedCash evt = cmd.ToEvent();
            AccountState newState = accountSt.Apply(evt);

            return (evt, newState);
        };
    }

    public static Transition<AccountState, AlteredOverdraft> SetOverdraft(SetOverdraft cmd)
    {
        return accountSt =>
        {
            AlteredOverdraft evt = cmd.ToEvent(cmd.Amount - accountSt.AllowedOverdraft);
            AccountState newState = accountSt.Apply(evt);

            return (evt, newState);
        };
    }
    //------------------------------------------------------------------------------Ʌ

    public static (Event Event, AccountState NewState) Debit_Old(this AccountState currentState, MakeTransfer transfer)
    {
        DebitedTransfer evt = transfer.ToEvent();  // can use `var` here, just for readability to use explicit event type here
        AccountState newState = currentState.Apply(evt);

        return (evt, newState);
    }

    public static Validation<(Event Event, AccountState NewState)> Debit(this AccountState currentState, MakeTransfer transfer)
    {
        if (currentState.Status != AccountStatus.Active)
            return Errors.AccountNotActive;

        if (currentState.Balance - transfer.Amount < currentState.AllowedOverdraft)
            return Errors.InsufficientBalance;

        var evt = transfer.ToEvent();
        AccountState newState = currentState.Apply(evt);

        return (evt, newState);
    }

    public static AccountState Create(CreatedAccount evt)  // CreatedAccount is a special case because there is no prior state
    {
        return new AccountState(Currency: evt.Currency, Status: AccountStatus.Active);
    }

    public static AccountState ToAccount(this CreatedAccount evt)  // same as above, but use extension method
    {
        return new AccountState(Currency: evt.Currency, Status: AccountStatus.Active);
    }

    public static AccountState Apply(this AccountState acc, Event evt)  // doesn't need to handle CreatedAccount because it is special
    {
        return evt switch
        {
            DepositedCash e => acc with { Balance = acc.Balance + e.Amount },
            DebitedTransfer e => acc with { Balance = acc.Balance - e.DebitedAmount },
            FrozeAccount _ => acc with { Status = AccountStatus.Frozen },
            _ => throw new InvalidOperationException()
        };
    }

    public static Option<AccountState> From(IEnumerable<Event> history)  // first event has to be CreatedAccount
    {
        return history.Match  // IEnumerable<T>.match call Head() internally (a little bit awkward since it doesn't look generic, but it actully make sense because of the return type) 
        (
           Empty: () => None,
           Otherwise: (created, otherEvents) =>
              Some(
                 otherEvents.Aggregate(seed: Account.Create((CreatedAccount)created), func: (state, evt) => state.Apply(evt))
              )
        );
    }
}

//-----------------------------------------------------------------------------------------------------V
public sealed record AccountState(CurrencyCode Currency, AccountStatus Status = AccountStatus.Requested,
                                  decimal Balance = 0m, decimal AllowedOverdraft = 0m);

public enum AccountStatus { Requested, Active, Frozen, Dormant, Closed }

public struct CurrencyCode
{
    string Value { get; }
    public CurrencyCode(string value) => Value = value;

    public static implicit operator string(CurrencyCode c) => c.Value;
    public static implicit operator CurrencyCode(string s) => new(s);

    public override string ToString() => Value;
}
//-----------------------------------------------------------------------------------------------------Ʌ
```

```C#
//-------------------------V
public static class Account
{
   public static AccountState Create(CreatedAccount evt)  // CreatedAccount is a special case because there is no prior state
   {
      return new AccountState(Currency: evt.Currency, Status: AccountStatus.Active);
   }

   public static AccountState Apply(this AccountState acc, Event evt)  // doesn't need to handle CreatedAccount because it is special
   {
      return evt switch
      {
         DepositedCash e => acc with { Balance = acc.Balance + e.Amount },
         DebitedTransfer e => acc with { Balance = acc.Balance - e.DebitedAmount },
         FrozeAccount _ => acc with { Status = AccountStatus.Frozen },
         _ => throw new InvalidOperationException()
      };
   }

   public static Option<AccountState> From(IEnumerable<Event> history)  // first event has to be CreatedAccount
   {
      return history.Match  // IEnumerable<T>.match call Head() internally (a little bit awkward since it is not generic, only fits even sourcing scenario) 
      (
         Empty: () => None,
         Otherwise: (created, otherEvents) =>
            Some(
               otherEvents.Aggregate(seed: Account.Create((CreatedAccount)created), func: (state, evt) => state.Apply(evt))
            )
      );
   }
}
//-------------------------Ʌ

//--------------------------------------------------------------V
public abstract record Event(Guid EntityId, DateTime Timestamp);

public record CreatedAccount(Guid EntityId, DateTime Timestamp, CurrencyCode Currency) : Event(EntityId, Timestamp);

public record FrozeAccount(Guid EntityId, DateTime Timestamp) : Event(EntityId, Timestamp);

public record DepositedCash(Guid EntityId, DateTime Timestamp, decimal Amount, Guid BranchId) : Event(EntityId, Timestamp);

public record DebitedTransfer(Guid EntityId, DateTime Timestamp,
                              string Beneficiary, string Iban, string Bic,
                              decimal DebitedAmount, string Reference) : Event(EntityId, Timestamp);
//--------------------------------------------------------------Ʌ
```

```C#
//-----------------------------------------------------------------------------------------------------V
public sealed rec൦rd AccountState(CurrencyCode Currency, AccountStatus Status = AccountStatus.Requested,
                                  decimal Balance = 0m, decimal AllowedOverdraft = 0m);

public enum AccountStatus { Requested, Active, Frozen, Dormant, Closed }

public struct CurrencyCode
{
   string Value { get; }
   public CurrencyCode(string value) => Value = value;

   public static implicit operator string(CurrencyCode c) => c.Value;
   public static implicit operator CurrencyCode(string s) => new(s);

   public override string ToString() => Value;
}
//-----------------------------------------------------------------------------------------------------Ʌ
```

```C#
public static Option<T> Head<T>(this IEnumerable<T> list)
{
   if (list == null)
      return None;

   var enumerator = list.GetEnumerator();
   return enumerator.MoveNext() ? Some(enumerator.Current) : None;
}

public static R Match<T, R>(this IEnumerable<T> list, Func<R> Empty, Func<T, IEnumerable<T>, R> Otherwise)
{
   return list.Head().Match(
      None: Empty,
      Some: head => Otherwise(head, list.Skip(1)));
}

public static Validation<T> ToValidation<T>(this Option<T> opt, Error error)
{
   return opt.Match(
      () => Invalid(error),
      (t) => Valid(t)
   );
}
```

```C#
public delegate Validation<(T, St)> Transition<St, T>(St state);

public static class Transition
{
    public static Transition<St, R> Select<St, T, R>(this Transition<St, T> transition, Func<T, R> project)
    {
        return state0 => transition(state0).Map(result => (project(result.Item1), result.Item2));
    }

    public static Transition<St, R> Bind_But_Author_Call_It_SelectMany<St, T, R>(this Transition<St, T> transition, Func<T, Transition<St, R>> f)
    {
        return state0 => transition(state0).Bind(t => f(t.Item1)(t.Item2));
    }

    public static Transition<St, RR> SelectMany<St, T, R, RR>(this Transition<St, T> transition, Func<T, Transition<St, R>> bind, Func<T, R, RR> project)
    {
        return state0 => transition(state0).Bind(t => bind(t.Item1)(t.Item2).Map(r => (project(t.Item1, r.Item1), r.Item2)));
    }
}
```

```C#
//----------------------------------V
public class EventSourcingController : ControllerBase
{
    Func<CreateAccountWithOptions, Validation<CreateAccountWithOptions>> validate;
    Func<Guid> generateId;
    Action<Event> saveAndPublish;

    public IActionResult CreateInitialized([FromBody] CreateAccountWithOptions cmd)
    {
       return 
          validate(cmd)
             .Bind(Initialize)
             .Match<IActionResult>
             (
                Invalid: errs => BadRequest(new { Errors = errs }),
                Valid: id => Ok(id)
             );
    }

    private Validation<Guid> Initialize(CreateAccountWithOptions cmd)  // note that there is reason to call this method "Initialize"
    {                                                                  // because the is no replay of historical events
        Guid id = generateId();
        DateTime now = DateTime.UtcNow;

        var create = new CreateAccount
        (
           Timestamp: now,
           AccountId: id,
           Currency: cmd.Currency
        );

        var depositCash = new AcknowledgeCashDeposit
        (
           Timestamp: now,
           AccountId: id,
           Amount: cmd.InitialDepositAccount,
           BranchId: cmd.BranchId
        );

        var setOverdraft = new SetOverdraft
        (
            Timestamp: now,
            AccountId: id,
            Amount: cmd.AllowedOverdraft
        );

        Transition<AccountState, IEnumerable<Event>> transitions =   // check chapter 10 if you forget how multiple from clauses used and how "unrelated" things e1, e2, e3
           from e1 in Account.Create(create)                         // (by "unrelated", I mean you don't need e1 to generate e2 or e2 to generate e3) work together
           from e2 in Account.Deposit(depositCash)
           from e3 in Account.SetOverdraft(setOverdraft)
           select List<Event>(e1, e2, e3);

        return transitions(default(AccountState))     // Validation<(IEnumerable<Event>, AccountState)>
           .Do(t => t.Item1.ForEach(saveAndPublish))  // still return Validation<(IEnumerable<Event>, AccountState)> because Do return @this
           .Map(_ => id);   // use discard as we just want wrap Guid(id) into Validation
    }
}

public rec൦rd CreateAccountWithOptions
(
   DateTime Timestamp,

   Boc.Domain.CurrencyCode Currency,  // remove Boc.Domain when copy to VS code
   decimal InitialDepositAccount,
   decimal AllowedOverdraft,
   Guid BranchId
) : Command(Timestamp);
//----------------------------------Ʌ
```


## `SelectMany` in Event Sourcing

Let's put Linq's query expression in details, to make things simplier, we only uses two `from` clauses 

```C#
public static class Account
{
   public static Transition<AccountState, CreatedAccount> Create(CreateAccount cmd)
   {
      return _ => 
      {
         CreatedAccount cA = cmd.ToEvent();  // cA is t.Item1
         AccountState asc = cA.ToAccount();  // asc is t.Item2
         return (cA, asc);
      };
   }

   public static Transition<AccountState, DepositedCash> Deposit(AcknowledgeCashDeposit cmd)
   {
      return accountSt =>
      {
         if (accountSt.Status != AccountStatus.Active)
            return Errors.AccountNotActive;

         DepositedCash dC = cmd.ToEvent();
         AccountState asd = accountSt.Apply(dC);  // newState is t.Item2

         return (dC, asd);
      };
   }   

   // ...
}

//--------------------------------------------------------------V
var create = new CreateAccount(...);
var depositCash = new AcknowledgeCashDeposit(...)

Transition<AccountState, IEnumerable<Event>> _ =  
   from e1 in Account.Create(create)  // e1 and create are always the same type and type of e1 is determined by create, check the side note of chapter 10
   from e2 in Account.Deposit(depositCash)
   select List<Event>(e1, e2);
//--------------------------------------------------------------Ʌ

//--------------------------------------------------------------V
public delegate Validation<(T, St)> Transition<St, T>(St state);

public static class Transition
{                                                 
   // return type is Transition<AccountState asd, List<Event>(cA, dC)>
   public static Transition<St, RR> SelectMany<St, T, R, RR>(this Transition<St, T> transition,  // Transition<AccountState ca, CreatedAccount asc>
                                                             Func<T, Transition<St, R>> bind,    // CreatedAccount asc => Transition<AccountState asd, DepositedCash dC> 
                                                             Func<T, R, RR> project)
   {
      return state0 => transition(state0)  // Validation<(CreatedAccount cA, AccountState asc)>  
         .Bind(t => bind(t.Item1)  // bind(t.Item1) is Transition<AccountState dC, DepositedCash asd> 
                    (t.Item2)      // Validation<(DepositedCash dC, AccountState asc)>
                    .Map(r => (project(t.Item1, r.Item1), r.Item2))  // Validation<`a<DepositedCash dC, AccountState asd>>
      );                                     
                      // t.Item1 is cA and is not used in `bind` (still being used in `project`), it is like you pass an unused argument to a local function
                      // t.Item2 is asc         r.Item1 is DepositedCash dC, r.Item2 is asd
                 // bind(t.Item1) is Account.Deposit(depositCash), i.e Transition<AccountState asd, DepositedCash dC>>
                             // project(t.Item1, r.Item1) is List<Event>(t.Item1, r.Item1) i.e List<Event>(e1, e2);
   }
}
//--------------------------------------------------------------Ʌ
```