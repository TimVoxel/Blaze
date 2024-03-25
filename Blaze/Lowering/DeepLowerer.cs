using Blaze.Binding;
using System.Collections.Immutable;

namespace Blaze.Lowering
{
    internal sealed class DeepLowerer : Lowerer
    {
        //HACK: probably shouldn't have to different lowerers
        //      should change the control flow graph instead

        public DeepLowerer() : base() { }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            //if ->
            //      execute if run function <body-subfunction>
            //if else ->
            //      execute if <condition> run function <body-subfunction>
            //      execute unless <condition> run function <else-body-subfunction>

            if (node.ElseBody == null)
            {
                var endLabel = GenerateLabel();
                var gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, true);
                var endLabelStatement = new BoundLabelStatement(endLabel);
                var result = new BoundBlockStatement(ImmutableArray.Create(gotoFalse, node.Body, endLabelStatement));
                return RewriteStatement(result);
            }
            else
            {
                var elseLabel = GenerateLabel();
                var endLabel = GenerateLabel();

                var gotoFalse = new BoundConditionalGotoStatement(elseLabel, node.Condition, true);
                var gotoEnd = new BoundGotoStatement(endLabel);
                var elseLabelStatement = new BoundLabelStatement(elseLabel);
                var endLabelStatement = new BoundLabelStatement(endLabel);
                var result = new BoundBlockStatement(ImmutableArray.Create(
                    gotoFalse,
                    node.Body,
                    gotoEnd,
                    elseLabelStatement,
                    node.ElseBody,
                    endLabelStatement
                ));
                return RewriteStatement(result);
            }
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            //source:
            //      execute if <condition> run function <body-subfunction>
            //
            //      <body-subfunction>
            //      statements
            //      execute if <condition> run function <body-subfunction>

            var checkLabel = GenerateLabel();
            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var checkLabelStatement = new BoundLabelStatement(checkLabel);
            var gotoCheck = new BoundGotoStatement(checkLabel);

            var gotoTrue = new BoundConditionalGotoStatement(node.ContinueLabel, node.Condition, false);
            var result = new BoundBlockStatement(ImmutableArray.Create(
                gotoCheck,
                continueLabelStatement,
                node.Body,
                checkLabelStatement,
                gotoTrue,
                breakLabelStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            //source:
            //      function <body-subfunction>
            //
            //      <body-subfunction>
            //      statements
            //      execute if <condition> run function <body-subfunction>

            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);

            var gotoTrue = new BoundConditionalGotoStatement(node.ContinueLabel, node.Condition, false);
            var result = new BoundBlockStatement(ImmutableArray.Create(
                continueLabelStatement,
                node.Body,
                gotoTrue,
                breakLabelStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteBreakStatement(BoundBreakStatement node)
        {
            return new BoundGotoStatement(node.Label);
        }

        protected override BoundStatement RewriteContinueStatement(BoundContinueStatement node)
        {
            return new BoundGotoStatement(node.Label);
        }
    }
}
