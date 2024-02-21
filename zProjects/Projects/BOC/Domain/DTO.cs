using Slowjams.Functional;

namespace BOC.Domain
{
   public record ResultDto<T>
   {
      public bool Succeeded { get; }
      public bool Failed => !Succeeded;
      public T Data { get; }
      public Error Error { get; }
      internal ResultDto(T data) => (Succeeded, Data) = (true, data);
      internal ResultDto(Error error) => Error = error;
   }
}
