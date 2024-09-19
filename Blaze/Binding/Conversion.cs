using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class Conversion
    {
        public static readonly Conversion None = new Conversion(false, false, false);
        public static readonly Conversion Identity = new Conversion(true, true, false);
        public static readonly Conversion Implicit = new Conversion(true, false, true);
        public static readonly Conversion Explicit = new Conversion(true, false, false);

        public bool Exists { get; private set; }
        public bool IsIdentity { get; private set; }
        public bool IsImplicit { get; private set; }
        public bool IsExplicit => Exists && !IsImplicit;                  

        public Conversion(bool exists, bool isIdentity, bool isImplicit)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            IsImplicit = isImplicit;
        }

        public static Conversion Classify(TypeSymbol from, TypeSymbol to)
        {
            if (from == to)
                return Identity;

            // any -> object
            if (from != TypeSymbol.Void && to == TypeSymbol.Object)
                return Implicit;

            // object -> any
            if (from == TypeSymbol.Object && to != TypeSymbol.Void)
                return Explicit;

            // int or bool -> string
            if (from == TypeSymbol.Int || from == TypeSymbol.Bool)
                if (to == TypeSymbol.String)
                    return Explicit;

            // enum -> string
            if (from is EnumSymbol e && to == TypeSymbol.String)
                return Explicit;
           
            /*
            // string -> int or bool
            if (from == TypeSymbol.String)        
                if (to == TypeSymbol.Int || to == TypeSymbol.Bool)
                    return Explicit;
            */
    
            return None;
        }
    }
}