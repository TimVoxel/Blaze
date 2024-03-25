using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class ControlFlowGraph
    {
        public BasicBlock Start { get; private set; }
        public BasicBlock End { get; private set; }
        public List<BasicBlock> Blocks { get; private set; }
        public List<BasicBlockBranch> Branches { get; private set; }

        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> block, List<BasicBlockBranch> branches)
        {
            Start = start;
            End = end;
            Blocks = block;
            Branches = branches;
        }

        public sealed class BasicBlock
        {
            public List<BoundStatement> Statements { get; private set;  } = new List<BoundStatement>();
            public List<BasicBlockBranch> Incoming { get; private set; } = new List<BasicBlockBranch>();
            public List<BasicBlockBranch> Outgoing { get; private set; } = new List<BasicBlockBranch>();

            public bool IsStart { get; private set; }
            public bool IsEnd { get; private set; } 

            public BasicBlock() { }

            public BasicBlock(bool isStart)
            {
                IsEnd = !isStart;
                IsStart = isStart;
            }

            public override string ToString()
            {
                if (IsStart)
                    return "<Start>";

                if (IsEnd)
                    return "<End>";

                using (var writer = new StringWriter())
                {
                    foreach (var statement in Statements)
                        statement.WriteTo(writer);

                    return writer.ToString();
                }
            }
        }

        public sealed class BasicBlockBranch
        {
            public BasicBlock From { get; private set; }
            public BasicBlock To { get; private set; }
            public BoundExpression? Condition { get; private set; }

            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpression? condition)
            {
                From = from;
                To = to;
                Condition = condition;
            }

            public override string ToString()
            {
                if (Condition == null)
                    return string.Empty;

                return Condition.ToString();
            }
        }

        public sealed class BasicBlockBuilder
        {
            private List<BasicBlock> _blocks = new List<BasicBlock>();
            private List<BoundStatement> _statements = new List<BoundStatement>();

            public List<BasicBlock> Build(BoundBlockStatement block)
            {
                foreach (var statement in block.Statements)
                {
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.ExpressionStatement:
                        case BoundNodeKind.VariableDeclarationStatement:
                        case BoundNodeKind.NopStatement:
                            _statements.Add(statement);
                            break;
                        case BoundNodeKind.GoToStatement:
                        case BoundNodeKind.ConditionalGotoStatement:
                        case BoundNodeKind.ReturnStatement:
                            _statements.Add(statement);
                            StartBlock();
                            break;
                        case BoundNodeKind.LabelStatement:
                            StartBlock();
                            _statements.Add(statement);
                            break;
                        default:
                            throw new Exception($"Unexpected statement {statement.Kind}");
                    }
                }

                EndBlock();
                return _blocks.ToList();
            }

            private void StartBlock() => EndBlock();

            private void EndBlock()
            {
                if (_statements.Any())
                {
                    var block = new BasicBlock();
                    block.Statements.AddRange(_statements);
                    _blocks.Add(block);
                    _statements.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private Dictionary<BoundStatement, BasicBlock> _blockFromStatement = new Dictionary<BoundStatement, BasicBlock>();
            private Dictionary<BoundLabel, BasicBlock> _blockFromLabel = new Dictionary<BoundLabel, BasicBlock>();

            private List<BasicBlockBranch> _branches = new List<BasicBlockBranch>();

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                BasicBlock start = new BasicBlock(true);
                BasicBlock end = new BasicBlock(false);

                if (!blocks.Any())
                    Connect(start, end);
                else
                    Connect(start, blocks.First());

                foreach (BasicBlock block in blocks)
                {
                    foreach (BoundStatement statement in block.Statements)
                    {
                        _blockFromStatement.Add(statement, block);
                        if (statement is BoundLabelStatement labelStatement)
                            _blockFromLabel.Add(labelStatement.Label, block);
                    }
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    BasicBlock current = blocks[i];
                    BasicBlock next = i == blocks.Count - 1 ? end : blocks[i + 1];

                    foreach (BoundStatement statement in current.Statements)
                    {
                        bool isLastStatement = statement == current.Statements.Last();
                        switch (statement.Kind)
                        {
                            case BoundNodeKind.ExpressionStatement:
                            case BoundNodeKind.VariableDeclarationStatement:
                            case BoundNodeKind.LabelStatement:
                            case BoundNodeKind.NopStatement:
                                if (isLastStatement)
                                    Connect(current, next);
                                break;
                            case BoundNodeKind.ReturnStatement:
                                Connect(current, end);
                                break;
                            case BoundNodeKind.GoToStatement:
                                BoundGotoStatement gotoStatement = (BoundGotoStatement)statement;
                                BasicBlock toBlock = _blockFromLabel[gotoStatement.Label];
                                Connect(current, toBlock);
                                break;
                            case BoundNodeKind.ConditionalGotoStatement:
                                BoundConditionalGotoStatement cgotoStatement = (BoundConditionalGotoStatement)statement;
                                BasicBlock thenBlock = _blockFromLabel[cgotoStatement.Label];
                                BoundExpression negatedCondition = Negate(cgotoStatement.Condition);
                                BoundExpression thenCondition = cgotoStatement.JumpIfFalse ? negatedCondition : cgotoStatement.Condition;
                                BoundExpression elseCondition = cgotoStatement.JumpIfFalse ? cgotoStatement.Condition : negatedCondition;
                                Connect(current, thenBlock, thenCondition);
                                Connect(current, next, elseCondition);
                                break;
                            default:
                                throw new Exception($"Unexpected statement {statement.Kind}");
                        }
                    }    
                }
            ScanAgain:
                foreach (BasicBlock block in blocks)
                {
                    if (!block.Incoming.Any())
                    {
                        RemoveBlock(blocks, block);
                        goto ScanAgain;
                    }
                }

                blocks.Insert(0, start);
                blocks.Add(end);

                return new ControlFlowGraph(start, end, blocks, _branches);
            }

            private BoundExpression Negate(BoundExpression condition)
            {
                if (condition is BoundLiteralExpression literal)
                {
                    bool value = (bool)literal.Value;
                    return new BoundLiteralExpression(!value);
                }

                BoundUnaryOperator unaryNegator = BoundUnaryOperator.SafeBind(SyntaxKind.ExclamationSignToken, TypeSymbol.Bool);
                return new BoundUnaryExpression(unaryNegator, condition);
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpression? condition = null)
            {
                if (condition is BoundLiteralExpression l)
                {
                    bool value = (bool)l.Value;
                    if (value)
                        condition = null;
                    else return;
                }

                BasicBlockBranch branch = new BasicBlockBranch(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                _branches.Add(branch);
            }

            private void RemoveBlock(List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (BasicBlockBranch branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    _branches.Remove(branch);
                }
                    
                foreach (BasicBlockBranch branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    _branches.Remove(branch);
                }   

                blocks.Remove(block);
            }
        }

        public void WriteTo(TextWriter writer)
        {
            string Quote(string text)
            {
                return "\"" + text.Replace("\"", "\\\"") + "\"";
            }

            writer.WriteLine("digraph G {");

            var blockIds = new Dictionary<BasicBlock, string>();
            for (int i = 0; i < Blocks.Count; i++)
            {
                var id = $"N{i}";
                blockIds.Add(Blocks[i], id);
            }

            foreach (var block in Blocks)
            {
                var id = blockIds[block];
                var label = Quote(block.ToString().Replace(Environment.NewLine, "\\l"));
                writer.WriteLine($"    {id} [label = {label} shape = box]");
            }

            foreach (var branch in Branches)
            {
                var fromId = blockIds[branch.From];
                var toId = blockIds[branch.To];
                var label = Quote(branch.ToString());
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph Create(BoundBlockStatement body)
        {
            var builder = new BasicBlockBuilder();
            var blocks = builder.Build(body);

            var graphBuilder = new GraphBuilder();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStatement body)
        {
            var graph = Create(body);

            foreach (var branch in graph.End.Incoming)
            {
                var last = branch.From.Statements.LastOrDefault();
                if (last == null || last.Kind != BoundNodeKind.ReturnStatement)
                    return false;
            }
            return true;
        }
    }
}