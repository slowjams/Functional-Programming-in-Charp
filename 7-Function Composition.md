Chapter 7-Designing Programs with Function Composition
==============================

C# doesn't have any special syntactic support for function composition, and although you could define a HOF Compose to compose two or more functions, this doesn't improve readability. This is why in C# it's best to resort to method chaining instead.

Let's define Bank of Codeland (BOC) online banking application whose specification for the workflow as follows:

1. Validate the requested transfer
2. Load the account
3. If the account has sufficient funds, debit the amount from the account
4. Persist the changes to the account
5. Wire the funds via the SWIFT network


## A Simple Workflow for Validation

The entire money transfer workflow is fairly complex, so to get us started, let's simplify it as follows:

1. Validate the requested transfer
2. Book the transfer (all subsequent steps 2 to 5 above)

skeleton code:

```C#
[ApiController]
[Route("[controller]")]
public class MakeTransferController : ControllerBase 
{
   IValidator<MakeTransfer> validator;

   [HttpPost] 
   [Route("api/MakeTransfer")]
   public void MakeTransfer([FromBody] MakeTransfer transfer)
   {
      if (validator.IsValid(transfer))   // use if, a sign that indicate imperative OO approach
         Book(transfer);
   }

   private void Book(MakeTransfer transfer) {
      // actually book the transfer...
   }
}

public interface IValidator<T> {
   bool IsValid(T t);
}
```

this approach is imperative OO approach, which uses `if`, a single `if` may look harmless, but if you start allowing one `if`, nothing is keeping you from having dozens of nested `if`s as additional requirements come in, and the complexity that ensues is what makes applications error-prone and difficult to reason about. Next, we'll look at how to use function composition instead.


## Refactoring with Data Flow in Mind

Let's try to think of the transfer request as data flowing through validation and into the Book method that performs the transfer:

```s
MakeTransfer -> Validate -> Book
```

There is a bit of a problem with types: Validate.IsValid returns a bool, whereas Book requires a `MakeTransfer` object, so these two functions don't compose.

Furthermore, we need to ensure that the request data flows through the validation and into Book only if it passes validation. This is where `Option` can help us: we can use
None to represent an invalid transfer request and `Some<MakeTransfer>` for a valid one:

```C#
public void MakeTransfer([FromBody] MakeTransfer transfer)
   => Some(transfer)
      .Where(validator.IsValid)
      .ForEach(book);
```

We *lift* the transfer data into an `Option` by using `Some()`, notice that, in doing so, we're expanding the meaning we give to `Option`, **we interpret `Some` not just to indicate the presence of data, but also the presence of valid data**.
Also. using `Where` which is the highly composable function allows us to glue everything together.


## Composition Leads to Greater Flexibility

Suppose you want to normalize the request before validating it so that things like whitespace and casing don't cause validation to fail then you just need to define a function that performs the new step and then integrate it into your workflow:

```C#
public void MakeTransfer([FromBody] MakeTransfer transfer)
   => Some(transfer)
      .Map(Normalize)   // plugs a new step into the workflow
      .Where(validator.IsValid)
      .ForEach(book);

private MakeTransfer Normalize(MakeTransfer request) => // ...
```


## An Introduction to Functional Domain Modeling

The following listing shows how an OO implementation could look:

```C#
//  in OOP, objects capture both data and behavior
public class Account
{
   public decimal Balance { get; private set; }
   
   public Account(decimal balance) { Balance = balance; }
   
   public void Debit(decimal amount)  // full of side effects
   {
      if (Balance < amount)
         throw new InvalidOperationException("Insufficient funds");
      Balance -= amount;
   }
}
```

In OOP, data and behavior live in the same object, and methods in the object can typically modify the object's state. By contrast,, **in FP data is captured with "dumb" data objects while behavior is encoded in functions**:

```C#
public rec൦rd AccountState(decimal Balance);  // an immutable record, only containing data

public static class Account 
{
   public static Option<AccountState> Debit(this AccountState current, decimal amount)  // Debit is a pure function now
   {
      return (current.Balance < amount) ? None : Some(new AccountState(current.Balance - amount));
   }
}
```

Notice how the OO implementation of `Debit` isn't composable: it has side effects and return `void`. The functional counterpart is completely different: it's a pure function and return a value, which can be used as input to the next function in the chain.


## An End-to-End Server-Side Workflow

Let's implement `Book` function, which should do the following:

1. Load the account
2. If the account has sufficient funds, debit the amount from the account
3. Persist the changes to the account
4. Wire the funds via the SWIFT network

Let's define two services that capture DB and SWIFT access:

```C#
//-----------------------------V
public interface IRepository<T> {
   Option<T> Get(Guid id);
   void Save(Guid id, T t);
}

public interface ISwiftService {
   void Wire(MakeTransfer transfer, AccountState account);
}

public rec൦rd AccountState(decimal Balance);  

public static class Account 
{
   public static Option<AccountState> Debit(this AccountState current, decimal amount) 
      => return (current.Balance < amount) ? None : Some(new AccountState(current.Balance - amount));
}
//-----------------------------Ʌ
```

Using these interfaces is still an OO pattern, but let's stick to it for now (you'll see how to use just functions in chapter 9)

```C#
public class MakeTransferController : ControllerBase
{
   private IValidator<MakeTransfer> validator;
   private IRepository<AccountState> accounts;
   private ISwiftService swift;

   public void MakeTransfer([FromBody] MakeTransfer transfer) 
   {
      Some(transfer)
      .Where(validator.IsValid)
      .ForEach(Book);
   }

   private void Book(MakeTransfer transfer)
   {
       Option<AccountState> optAccountState = accounts.Get(transfer.DebitedAccountId)
          .Bind(account => account.Debit(transfer.Amount));

       optAccountState.ForEach(account =>
       {
          accounts.Save(transfer.DebitedAccountId, account);
          swift.Wire(transfer, account);
       });
   }
}
```

You can see that with FP approach, there is no `if` statements, and we isolate side effect to the end of the workflow `ForEach` who doesn't have a useful return value, so that's where the pipeline ends. In our example, the only statments are the two (`IRepository.Save()` and `ISwiftService.Wire`) within the last `ForEach`, this is fine because we want to have two side effects-there is no point hiding that.