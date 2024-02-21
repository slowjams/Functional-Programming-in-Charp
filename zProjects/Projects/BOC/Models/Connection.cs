namespace BOC.Models
{
   public record ConnectionString(string Value)
   {
      public static implicit operator string(ConnectionString c) => c.Value;
      public static implicit operator ConnectionString(string s) => new(s);
   }

   public record SqlTemplate(string Value)
   {
      public static implicit operator string(SqlTemplate c) => c.Value;
      public static implicit operator SqlTemplate(string s) => new(s);

      public override string ToString() => Value;
   }
}
