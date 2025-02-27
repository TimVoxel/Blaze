﻿using Blaze.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Blaze.Emit.Nodes
{
    public sealed class MinecraftFunction : StructureEmittionNode
    {
        public class Builder
        {
            private string? _callName;
            public string CallName
            {
                get
                {
                    //If the function is a fabricated sub function, 
                    //It's call name would be different to one that is
                    //Contained in the symbol

                    if (Function == null)
                        throw new Exception("Function emittion without a function symbol");

                    if (_callName != null)
                        return _callName;

                    if (SubFunctionKind == null)
                    {
                        FunctionSymbol functionSymbol = (FunctionSymbol)Function;
                        _callName = functionSymbol.AddressName;
                        return _callName;
                    }
                    else
                    {
                        var builder = new StringBuilder();
                        var stack = new Stack<IMemberSymbol>();

                        var previous = Function.Parent;
                        while (previous != null)
                        {
                            if (previous is NamespaceSymbol ns && ns.IsGlobal)
                                break;
                            stack.Push(previous);
                            previous = previous.Parent;
                        }

                        while (stack.Any())
                            builder.Append(stack.Pop().Name + "/");

                        builder.Append(Name);
                        _callName = builder.ToString();
                        return _callName;
                    }
                }
            }

            private int _loopCount = 0;
            private int _ifCount = 0;
            private int _elseCount = 0;
            
            public string Name { get; }
            public ImmutableArray<TextEmittionNode>.Builder Content { get; } = ImmutableArray.CreateBuilder<TextEmittionNode>();
            public ImmutableArray<Builder>.Builder SubFunctions { get; } = ImmutableArray.CreateBuilder<Builder>();
            public ImmutableArray<EmittionVariableSymbol>.Builder Locals { get; } = ImmutableArray.CreateBuilder<EmittionVariableSymbol>();
            public FunctionSymbol Function { get; }
            public SubFunctionKind? SubFunctionKind { get; }

            public Builder(string name, FunctionSymbol function, SubFunctionKind? kind)
            {
                Name = name;
                Function = function;
                SubFunctionKind = kind;
            }

            public void AddLocal(EmittionVariableSymbol emittionVariableSymbol)
            {
                if (!Locals.Contains(emittionVariableSymbol))
                    Locals.Add(emittionVariableSymbol);
            }

            public MinecraftFunction ToFunction()
            {
                var subBuilder = ImmutableArray.CreateBuilder<MinecraftFunction>();

                foreach (var builder in SubFunctions)
                    subBuilder.Add(builder.ToFunction());

                return new MinecraftFunction(Name, Function, SubFunctionKind, Content.ToImmutable(), subBuilder.ToImmutable(), Locals.ToImmutable());
            }

            public Builder CreateSub(SubFunctionKind kind)
            {
                var name = string.Empty;
                switch (kind)
                {
                    case Emit.SubFunctionKind.If:
                        name = $"{Name}_sif{_ifCount++}";
                        break;
                    case Emit.SubFunctionKind.Else:
                        name = $"{Name}_sel{_elseCount++}";
                        break;
                    case Emit.SubFunctionKind.Loop:
                        name = $"{Name}_sl{_loopCount++}";
                        break;
                    default:
                        throw new Exception($"Unexpected sub function kind {kind}");
                }

                Debug.Assert(Function != null);
                var sub = new Builder(name, Function, kind);
                SubFunctions.Add(sub);
                return sub;
            }

            public Builder CreateSubNamed(string name)
            {
                var fullName = $"{Name}_{name}";
                var sub = new Builder(fullName, Function, Emit.SubFunctionKind.Misc);
                SubFunctions.Add(sub);
                return sub;
            }

            public void AddCommand(CommandNode command) => Content.Add(command);
            public void AddLineBreak() => Content.Add(TextTriviaNode.LineBreak());
            public void AddComment(string text) => Content.Add(TextTriviaNode.Comment(text));

            //Temporary
            public void AddCommand(string command, bool isCleanUp = false) => Content.Add(new TextCommand(command, isCleanUp));
            public void AddMacro(string command) => Content.Add(new TextCommand($"${command}", false));
        }

        public SubFunctionKind? SubFunctionKind { get; }

        public ImmutableArray<TextEmittionNode> Content { get; }
        public ImmutableArray<MinecraftFunction> SubFunctions { get; }
        public ImmutableArray<EmittionVariableSymbol> Locals { get; } 

        //public ImmutableArray<EmittionVariableSymbol> Variables { get; }

        public override EmittionNodeKind Kind => EmittionNodeKind.MinecraftFunction;

        private string? _callName;
        public string CallName
        {
            get
            {
                //If the function is a fabricated sub function, 
                //It's call name would be different to one that is
                //Contained in the symbol

                if (Symbol == null)
                    throw new Exception("Function emittion without a function symbol");

                if (_callName != null)
                    return _callName;

                if (SubFunctionKind == null)
                {
                    FunctionSymbol functionSymbol = (FunctionSymbol)Symbol;
                    _callName = functionSymbol.AddressName;
                    return _callName;
                }
                else
                {
                    var builder = new StringBuilder();
                    var stack = new Stack<IMemberSymbol>();

                    var previous = Symbol.Parent;
                    while (previous != null)
                    {
                        if (previous is NamespaceSymbol ns && ns.IsGlobal)
                            break;
                        stack.Push(previous);
                        previous = previous.Parent;
                    }

                    while (stack.Any())
                        builder.Append(stack.Pop().Name + "/");

                    builder.Append(Name);
                    _callName = builder.ToString();
                    return _callName;
                }
            }
        }

        private MinecraftFunction(string name, IMemberSymbol symbol, SubFunctionKind? subFunctionKind, ImmutableArray<TextEmittionNode> content, ImmutableArray<MinecraftFunction> sub, ImmutableArray<EmittionVariableSymbol> locals) : base(symbol, name)
        {
            SubFunctionKind = subFunctionKind;
            Content = content;
            SubFunctions = sub;
            Locals = locals;
            //Variables = new List<EmittionVariableSymbol>();
        }

        public static Builder Init(NamespaceSymbol globalNamespace)
        {
            var init = new FunctionSymbol("init", globalNamespace, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, true, false, AccessModifier.Private, null);
            return new Builder(init.Name, init, null);
        }

        public static Builder Tick(NamespaceSymbol globalNamespace)
        {
            var tick = new FunctionSymbol("tick", globalNamespace, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, false, true, AccessModifier.Private, null);
            return new Builder(tick.Name, tick, null);
        }

        public CommandNode GetCall(string rootNamespace) => new TextCommand($"function {rootNamespace}:{FullName}", false);
    }

    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
