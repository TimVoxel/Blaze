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

            // int, float, double, bool -> string
            if (from == TypeSymbol.Int || from == TypeSymbol.Bool)
                if (to == TypeSymbol.String)
                    return Explicit;

            //float -> double
            if (from == TypeSymbol.Float && to == TypeSymbol.Double)
                return Implicit;

            //double -> float
            if (from == TypeSymbol.Double && to == TypeSymbol.Float)
                return Implicit;

            //double, float -> int
            if (from == TypeSymbol.Float || from == TypeSymbol.Double && to == TypeSymbol.Int)
                return Explicit;

            //int -> double, float
            if (from == TypeSymbol.Int && to == TypeSymbol.Float || to == TypeSymbol.Double)
                return Implicit;

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