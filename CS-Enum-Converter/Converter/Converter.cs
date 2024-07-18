using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CSEnumConverter.Converter
{
    internal class Converter
    {
        private readonly CSharpSolution _solution = new CSharpSolution();
        private Enum _enum;

        private DocumentEditor _currEditor;
        private readonly List<SyntaxNode> _changedNodes = new List<SyntaxNode>();
        private int _replacedNodeCount = 0;

        public void OpenEditInVisualStudio(int logLineNumber)
        {
            _solution.OpenLocation(logLineNumber);
        }

        /// <summary>
        /// Replaces all literals with the corresponding enum values in the provided solution.
        /// </summary>
        /// <param name="enumCode">The code representing the enum.</param>
        /// <param name="solutionPath">The path to the solution.</param>
        public void ConvertCode(string enumCode, string solutionPath)
        {
            try
            {
                InitVariables();

                _enum.ParseCode(enumCode);

                _solution.OpenSolution(solutionPath);

                var _locationFinder = new EnumLocationFinder(_enum, _solution);
                INamedTypeSymbol enumSymbol = _locationFinder.FindEnum();

                if (enumSymbol == null)
                    return;

                var locations = _locationFinder.GetEnumReferences(enumSymbol);
                var locationGroups = locations.Distinct().GroupBy(x => x.Document.Project.Id);

                foreach (var projectLocation in locationGroups)
                {
                    AnalyzeProject(projectLocation.Key, projectLocation.ToList());
                }

                _solution.Workspace.TryApplyChanges(_solution.CSSolution);

                Logger.WriteLine(string.Format("Replaced {0} nodes", _replacedNodeCount), 0, Color.Lime);
                if (Logger.WarningCount > 0)
                    Logger.WriteLine(string.Format("Warnings: {0}", Logger.WarningCount), 0, Color.Yellow);
                if (Logger.ErrorCount > 0)
                    Logger.WriteLine(string.Format("Errors: {0}", Logger.ErrorCount), 0, Color.Tomato);

                Logger.SaveLog();

                _solution.Close();
            }
            catch (Exception e)
            {
                Logger.WriteError(string.Format("An Exception has occured: {0}", e));
            }
        }

        private void InitVariables()
        {
            _enum = new Enum();
            _changedNodes.Clear();
            _replacedNodeCount = 0;
        }

        /// <summary>
        /// Analyzes the project with the specified projectId and enum locations.
        /// </summary>
        /// <param name="projectId">The ID of the project to analyze.</param>
        /// <param name="locations">The list of enum reference locations.</param>
        private void AnalyzeProject(ProjectId projectId, List<ReferenceLocation> locations)
        {
            var project = _solution.CSSolution.GetProject(projectId);
            var compilation = _solution.GetCompilation(projectId);

            Logger.WriteLine(string.Format("Analyzing project: {0}", project.Name), 0, Color.Magenta);

            var locationGroup = locations.GroupBy(x => x.Document.Id);
            foreach (var documentLocation in locationGroup)
            {
                AnalyzeFile(ref project, compilation, documentLocation.Key, documentLocation.ToList());
            }

            _solution.CSSolution = project.Solution;
        }

        /// <summary>
        /// Analyzes the file with the specified project, compilation, documentId, and locations.
        /// </summary>
        /// <param name="project">The project to analyze.</param>
        /// <param name="compilation">The compilation of the project.</param>
        /// <param name="documentId">The ID of the document to analyze.</param>
        /// <param name="locations">The list of reference locations.</param>
        private void AnalyzeFile(ref Project project, Compilation compilation, DocumentId documentId, List<ReferenceLocation> locations)
        {
            var document = project.GetDocument(documentId);
            var tree = document.GetSyntaxTreeAsync().Result;
            SemanticModel model = compilation.GetSemanticModel(tree);

            _currEditor = DocumentEditor.CreateAsync(document).Result;

            Logger.WriteLine(string.Format("Analyzing file: {0}", document.Name), 0, Color.Lime);

            foreach (var location in locations)
            {
                AnalyzeLocation(location, model);
            }

            try
            {
                document = _currEditor.GetChangedDocument();
                project = document.Project;
            }
            catch (Exception e)
            {
                Logger.WriteError(string.Format("Could not create changed document: {0}", e));
            }
        }

        /// <summary>
        /// Analyzes the enum reference location.
        /// </summary>
        /// <param name="location">The enum reference location to analyze.</param>
        /// <param name="model">The semantic model.</param>
        private void AnalyzeLocation(ReferenceLocation location, SemanticModel model)
        {
            var enumNode = (IdentifierNameSyntax)Helper.GetNodeFromLocation(location);
            var methodDeclaration = enumNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            var variableDeclaration = enumNode.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();
            var parameterNode = enumNode.Ancestors().OfType<ParameterSyntax>().FirstOrDefault();
            var assignmentExpression = enumNode.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
            var ifStatement = enumNode.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault();
            var argumentNode = enumNode.Ancestors().OfType<ArgumentSyntax>().FirstOrDefault();
            var switchStatement = enumNode.Ancestors().OfType<SwitchStatementSyntax>().FirstOrDefault();
            var invocationExpression = enumNode.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            var propertyDeclaration = enumNode.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();

            if (variableDeclaration != null) // Declaration of local variables
            {
                AnalyzeDeclarationAssignment(variableDeclaration, model);
            }
            else if (parameterNode != null) // Declaration of a parameter
            {
                AnalyzeParameterAssignment(parameterNode, model);
            }
            else if (ifStatement?.Condition.DescendantNodesAndSelf().Contains(enumNode) == true) // If statement
            {
                AnalyzeIfStatement(ifStatement, model, enumNode.Identifier);
            }
            else if (switchStatement?.Expression.DescendantNodesAndSelf().Contains(enumNode) == true) // switch case statements
            {
                AnalazeSwitchStatement(switchStatement, model, enumNode.Identifier);
            }
            else if (methodDeclaration?.ReturnType.DescendantNodesAndSelf().Contains(enumNode) == true) // Return value
            {
                AnalyzeFunctionReturnValue(methodDeclaration, model);
            }
            else if (argumentNode != null) // Method argument
            {
                AnalyzeArgument(argumentNode, model, enumNode.Identifier);
            }
            else if (invocationExpression != null) // Method invocations
            {
                AnalyzeFunctionCall(invocationExpression, model);
            }
            else if (assignmentExpression != null) // Assignment expressions
            {
                AnalyzeAssignment(assignmentExpression, model, enumNode.Identifier);
            }
            else if (propertyDeclaration != null)
            {
                // Dont analyze property blocks 
            }
            else
            {
                Logger.WriteError(string.Format("Could not Analyze Location: {0}", location.Location.ToString()), 1, enumNode);
            }
        }

        /// <summary>
        /// Analyzes the if statement and replaces the enum variable with the corresponding enum values.
        /// </summary>
        /// <param name="ifStatement">The if statement to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <param name="enumVariableName">The identifier name of the enum</param>
        private void AnalyzeIfStatement(IfStatementSyntax ifStatement, SemanticModel model, SyntaxToken enumVariableName)
        {
            var condition = ifStatement.Condition;
            var identifierNodes = Helper.GetAllIdentifiersByName(condition, enumVariableName.ValueText);
            if (!identifierNodes.Any())
                return;

            Logger.WriteLine(string.Format("Analyzing if statement: {0}", condition), 1, Color.LightSkyBlue, condition);

            // Get all binaryExpressions in the ifStatement which contain the variable identifier
            var binaryExpressions = new List<BinaryExpressionSyntax>();
            foreach (var identifierNode in identifierNodes)
            {
                var expressionStatement = Helper.GetHighestBinaryExpression(identifierNode);
                if (expressionStatement != null)
                    binaryExpressions.Add(expressionStatement);
            }

            // Check if the variable identifiers are divided by an AndAlso or an OrElse expression
            if (binaryExpressions.Count != binaryExpressions.Distinct().Count())
            {
                Logger.WriteWarning(string.Format("Variable \"{0}\" is used multiple times in the if statement but is not divided by an && or an || expression. Following node will not be replaced: {1}", enumVariableName, ifStatement.Condition), 2, ifStatement.Condition);
                return;
            }

            foreach (var binaryExpression in binaryExpressions)
            {
                if (binaryExpression.DescendantNodes().OfType<BinaryExpressionSyntax>().Any(node => node.IsKind(SyntaxKind.LogicalAndExpression)))
                {
                    Logger.WriteWarning(string.Format("Perhaps incorrect parentheses in if statement. Following node will not be replaced: {0}", binaryExpression), 2, binaryExpression);
                    break;
                }

                AnalyzeExpressionNode(binaryExpression, model, enumVariableName);
            }
        }

        /// <summary>
        /// Replaces an expression with an HasFlags() statement if possible
        /// </summary>
        /// <param name="expressionNode">The expression to replace</param>
        /// <param name="model">The semantic model</param>
        /// <param name="enumVariableName">The identifier name of the enum</param>
        /// <returns>true if the expression has been replaced</returns>
        private bool ReplaceExpressionWithHasFlag(BinaryExpressionSyntax expressionNode, SemanticModel model, SyntaxToken? enumVariableName)
        {
            if (expressionNode == null)
                return false;

            if (CheckForVariableInExpression(expressionNode, enumVariableName))
                return true; // Return true to prevent further analysis

            // Check if the right side of the expression is a number
            var rightNumber = Helper.GetConstantNumberFromExpression(expressionNode.Right, model);
            if (rightNumber == null)
                return false;

            // Check if the left side of the expression in a paranthesized expression
            if (!(expressionNode.Left is ParenthesizedExpressionSyntax parenthesizedExpression))
                return false;

            if (!(parenthesizedExpression.Expression is BinaryExpressionSyntax leftExpression) || !leftExpression.IsKind(SyntaxKind.BitwiseAndExpression))
                return false;

            // Retrieve the number of the left side of the expression
            var invocationParameter = (leftExpression.Right as InvocationExpressionSyntax)?.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            var leftNumberExpression = Helper.GetConversionMethodInvocation(invocationParameter) != null ? invocationParameter : leftExpression.Right;
            if (leftNumberExpression is PrefixUnaryExpressionSyntax unaryExpression && unaryExpression.IsKind(SyntaxKind.BitwiseNotExpression))
                return false;

            var leftNumber = Helper.GetConstantNumberFromExpression(leftNumberExpression, model);
            if (leftNumber == null)
                return false;

            // Check the type of comparison
            var isGreaterExpression = expressionNode.IsKind(SyntaxKind.GreaterThanExpression) && rightNumber == 0;  // var & flag > 0
            var isAndEqualsExpression = expressionNode.IsKind(SyntaxKind.EqualsExpression) && leftNumber == rightNumber; // var & flag = flag
            var isNotExpression = expressionNode.IsKind(SyntaxKind.EqualsExpression) && leftNumber != 0 && rightNumber == 0; // var & flag = 0
            if (!(isGreaterExpression || isAndEqualsExpression || isNotExpression))
                return false;

            var variable = leftExpression.Left;

            if (variable.DescendantNodes().OfType<BinaryExpressionSyntax>().Any() || variable.DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Any())
                return false;

            // Remove type conversion methods from the variable
            invocationParameter = (variable as InvocationExpressionSyntax)?.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            if (invocationParameter != null)
            {
                if (Helper.GetConversionMethodInvocation(invocationParameter) != null)
                    variable = invocationParameter;
                else
                    return false;
            }

            variable = Helper.RemoveCastExpressions(variable);
            variable = Helper.RemoveParenthesizedExpressions(variable);

            // Check if a node has already been replaced
            if (_changedNodes.Contains(expressionNode))
            {
                Logger.WriteError(string.Format("The following node has already been replaced: {0}", expressionNode), 2, expressionNode);
                return true;
            }

            // Replace the expression with a HasFlag() statement
            string enumText = _enum.GetEntriesStringFromNumber((int)leftNumber, out _);
            string nodeText = $"{variable}.HasFlag({enumText})";
            if (isNotExpression)
                nodeText = "!" + nodeText;

            var newNode = SyntaxFactory.ParseExpression(nodeText);

            var trailingTrivia = expressionNode.GetTrailingTrivia();
            newNode = newNode.WithTrailingTrivia(trailingTrivia);

            _currEditor.ReplaceNode(expressionNode, newNode);

            _replacedNodeCount++;
            _changedNodes.Add(expressionNode);

            Logger.WriteLine(string.Format("Replaced node: {0} with: {1}", expressionNode, newNode), 2, node: expressionNode);

            return true;
        }

        /// <summary>
        /// Replaces all nodes in the switch statement and the case sections with the corresponding enum values.
        /// </summary>
        /// <param name="switchStatement">The switch statement to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <param name="enumVariableName">The identifier name of the enum</param>
        private void AnalazeSwitchStatement(SwitchStatementSyntax switchStatement, SemanticModel model, SyntaxToken enumVariableName)
        {
            Logger.WriteLine(string.Format("Analyzing switch statement: {0}", switchStatement.Expression), 1, Color.LightSkyBlue, switchStatement.Expression);

            // Analyze the switch statement
            var identifierNodes = Helper.GetAllIdentifiersByName(switchStatement.Expression, enumVariableName.ValueText);
            foreach (var identifierNode in identifierNodes)
            {
                var expressionStatement = Helper.GetHighestBinaryExpression(identifierNode);
                if (expressionStatement != null)
                {
                    AnalyzeExpressionNode(expressionStatement, model, enumVariableName);
                }
            }

            // Analyze the case sections
            foreach (var caseSection in switchStatement.Sections)
            {
                foreach (var label in caseSection.Labels)
                {
                    Logger.WriteLine(string.Format("Analyzing case label: {0}", label), 2, Color.LightSteelBlue, label);
                    var expression = label.DescendantNodes().OfType<ExpressionSyntax>().FirstOrDefault();
                    if (expression != null)
                        AnalyzeExpressionNode(expression, model, enumVariableName);
                }
            }
        }

        /// <summary>
        /// Replaces all return values of a function with the corresponding enum values.
        /// </summary>
        /// <param name="methodStatement">The method statement to analyze</param>
        /// <param name="model">The semantic model</param>
        private void AnalyzeFunctionReturnValue(MethodDeclarationSyntax methodStatement, SemanticModel model)
        {
            // Analyze return keywords
            var returnStatements = methodStatement.DescendantNodes().OfType<ReturnStatementSyntax>().ToList();
            foreach (var returnStatement in returnStatements)
            {
                Logger.WriteLine(string.Format("Analyzing return statement: {0}", returnStatement), 1, Color.LightSkyBlue, returnStatement);
                AnalyzeExpressionNode(returnStatement.Expression, model, null, false);
            }
        }

        /// <summary>
        /// Replaces all arguments of a method with the corresponding enum values.
        /// </summary>
        /// <param name="argumentNode">The argument to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <param name="enumVariableName">The identifier name of the enum</param>
        /// <param name="indentLevel">Indentation level for the Logger</param>
        private void AnalyzeArgument(ArgumentSyntax argumentNode, SemanticModel model, SyntaxToken? enumVariableName, int indentLevel = 1)
        {
            argumentNode = argumentNode.AncestorsAndSelf().OfType<ArgumentSyntax>().LastOrDefault();

            // Check if the argument is part of a special function
            var invocationNode = argumentNode.Ancestors().OfType<InvocationExpressionSyntax>().LastOrDefault();
            if (invocationNode != null)
            {
                if (AnalyzeSpecialFunctions(invocationNode, model))
                    return;
            }

            ExpressionSyntax expression = Helper.GetHighestBinaryExpression(argumentNode) ?? argumentNode.DescendantNodes().OfType<ExpressionSyntax>().FirstOrDefault();
            Color color = indentLevel > 1 ? Color.LightSteelBlue : Color.LightSkyBlue;
            Logger.WriteLine(string.Format("Analyzing argument expression: {0}", expression), indentLevel, color, argumentNode);

            AnalyzeExpressionNode(expression, model, enumVariableName, false);
        }

        /// <summary>
        /// Replaces all arguments of the invocation with the corresponding enum values.
        /// </summary>
        /// <param name="invocationNode">The function invocation to analyze</param>
        /// <param name="model">The semantic model</param>
        private void AnalyzeFunctionCall(InvocationExpressionSyntax invocationNode, SemanticModel model)
        {
            if (AnalyzeSpecialFunctions(invocationNode, model))
                return;

            var methodSymbol = _solution.GetSymbolInfo(invocationNode).Symbol;
            Logger.WriteLine(string.Format("Analyzing function call: {0}", invocationNode), 1, Color.LightSkyBlue, invocationNode);

            foreach (var argument in invocationNode.ArgumentList.Arguments)
            {
                var argumentType = _solution.GetTypeInfo(argument.Expression).Type;
                if (SymbolEqualityComparer.Default.Equals(argumentType, _enum.EnumSymbol))
                {
                    AnalyzeArgument(argument, model, null, 2);
                }
            }
        }

        /// <summary>
        /// Analyzes special functions and replaces specific arguments of them.
        /// </summary>
        /// <param name="invocationNode">The function invocation to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <returns>true, if it found a special function</returns>
        private bool AnalyzeSpecialFunctions(InvocationExpressionSyntax invocationNode, SemanticModel model)
        {
            var methodIdentifier = invocationNode.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault()?.Identifier;
            methodIdentifier = methodIdentifier ?? invocationNode.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault().Name.Identifier;

            if (methodIdentifier == null)
                return false;

            string methodName = methodIdentifier.Value.ValueText;

            if (methodName == "ALLG_SetzeBit") // The second parameter of the method ALLG_SetzeBit is always the bit value
            {
                Logger.WriteLine(string.Format("Analyzing special function: {0}", invocationNode), 1, Color.LightSkyBlue, invocationNode);
                const int argumentIndex = 1;
                if (invocationNode.ArgumentList.Arguments.Count - 1 >= argumentIndex)
                {
                    var argument = invocationNode.ArgumentList.Arguments[argumentIndex];
                    var expression = argument.DescendantNodes().OfType<ExpressionSyntax>().FirstOrDefault();
                    AnalyzeExpressionNode(expression, model, null, false);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces the assignment value of variable declaration default values with the corresponding enum values.
        /// </summary>
        /// <param name="declarationNode">The variable declaration to analyze</param>
        /// <param name="model">The semantic model</param>
        private void AnalyzeDeclarationAssignment(VariableDeclarationSyntax declarationNode, SemanticModel model)
        {
            var variableDeclarator = declarationNode.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            var initializer = variableDeclarator.Initializer;
            if (initializer != null)
            {
                Logger.WriteLine(string.Format("Analyzing declaration: {0}", declarationNode), 1, Color.LightSkyBlue, declarationNode);

                var enumVariableName = variableDeclarator.Identifier;
                var expressionNode = initializer.Value;
                AnalyzeExpressionNode(expressionNode, model, enumVariableName, false);
            }
        }

        // Analyzes the assignment value of parameter declaration default values
        /// <summary>
        /// Replaces the assignment value of parameter declaration default values with the corresponding enum values.
        /// </summary>
        /// <param name="parameterNode">The parameter declaration to analyze</param>
        /// <param name="model">The semantic model</param>
        private void AnalyzeParameterAssignment(ParameterSyntax parameterNode, SemanticModel model)
        {
            var initializer = parameterNode.DescendantNodesAndSelf().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
            if (initializer != null)
            {
                Logger.WriteLine(string.Format("Analyzing parameter: {0}", parameterNode), 1, Color.LightSkyBlue, parameterNode);

                var expressionNode = initializer.Value;
                var enumVariableName = parameterNode.Identifier;
                AnalyzeExpressionNode(expressionNode, model, enumVariableName, false);
            }
        }

        /// <summary>
        /// Replaces the right side of an assignment with the corresponding enum values.
        /// </summary>
        /// <param name="assignment">The assignment syntax to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <param name="enumVariableName">The identifer name of the enum</param>
        private void AnalyzeAssignment(AssignmentExpressionSyntax assignment, SemanticModel model, SyntaxToken enumVariableName)
        {
            Logger.WriteLine(string.Format("Analyzing assignment: {0}", assignment), 1, Color.LightSkyBlue, assignment);

            // Check if the right side of the assignment is a special function
            var invocationExpression = assignment.Right.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocationExpression != null && AnalyzeSpecialFunctions(invocationExpression, model))
                return;

            AnalyzeExpressionNode(assignment.Right, model, enumVariableName, false);
        }

        /// <summary>
        /// Replaces expressions like AND, OR, ADD etc. with the corresponding enum values.
        /// </summary>
        /// <param name="expressionNode">The expression node to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <param name="enumVariableName">The identifier name of the enum</param>
        /// <param name="canAddParentheses">Whether parentheses can be added to encapsulate the enum values</param>
        void AnalyzeExpressionNode(ExpressionSyntax expressionNode, SemanticModel model, SyntaxToken? enumVariableName, bool canAddParentheses = true)
        {
            if (expressionNode is CastExpressionSyntax castExpression)
                expressionNode = castExpression.Expression;

            expressionNode = Helper.RemoveParenthesizedExpressions(expressionNode);

            // Simple number
            if (expressionNode is LiteralExpressionSyntax)
            {
                var value = model.GetConstantValue(expressionNode);
                if (value.HasValue && Helper.IsNumberOrChar(value.Value))
                {
                    ReplaceNodeWithEnumValues(expressionNode, Convert.ToInt32(value.Value), canAddParentheses);
                    return;
                }
            }

            if (ReplaceExpressionWithHasFlag(expressionNode as BinaryExpressionSyntax, model, enumVariableName))
                return;

            if (CheckForVariableInExpression(expressionNode, enumVariableName))
                return;

            // Binary expressions like and, or, add
            var binaryExpressions = expressionNode.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().ToList();
            var skipBinaryExpressions = new List<ExpressionSyntax>();
            if (binaryExpressions.Any())
            {
                foreach (var binaryExpression in binaryExpressions.ToList())
                {
                    if (skipBinaryExpressions.Contains(binaryExpression))
                        continue;

                    var value = model.GetConstantValue(binaryExpression);
                    if (value.HasValue && Helper.IsNumberOrChar(value.Value))
                    {
                        ReplaceNodeWithEnumValues(binaryExpression, Convert.ToInt32(value.Value), canAddParentheses);

                        // Remove all already replaced children
                        skipBinaryExpressions.AddRange(binaryExpression.DescendantNodes().OfType<BinaryExpressionSyntax>());
                    }
                    else
                    {
                        var filteredExpression = FilterBinaryExpression(binaryExpression, model);
                        if (filteredExpression == null)
                            continue;

                        filteredExpression = Helper.RemoveParenthesizedExpressions(filteredExpression);

                        if (CheckForDuplicateBits(binaryExpression, model))
                        {
                            Logger.WriteWarning(string.Format("Found duplicate bits. Following node will not be replaced: {0}", expressionNode), 2, expressionNode);
                            break;
                        }

                        ReplaceNumericalExpressions(filteredExpression, model);
                        skipBinaryExpressions.Add(filteredExpression);
                    }
                }
            }

            // Not epxressions
            var notExpressions = expressionNode.DescendantNodesAndSelf().OfType<PrefixUnaryExpressionSyntax>().Where(node => node.IsKind(SyntaxKind.BitwiseNotExpression)).ToList();
            foreach (var notExpression in notExpressions)
            {
                var operandNode = notExpression.Operand;
                var value = model.GetConstantValue(operandNode);
                if (value.HasValue && Helper.IsNumberOrChar(value.Value))
                {
                    ReplaceNodeWithEnumValues(operandNode, Convert.ToInt32(value.Value));
                }
            }
        }

        /// <summary>
        /// Filter binary expressions to analyze
        /// </summary>
        /// <param name="binaryExpression">The binary expression to filter</param>
        /// <param name="model">The semantic model</param>
        /// <returns>The filtered binary expression or null</returns>
        ExpressionSyntax FilterBinaryExpression(BinaryExpressionSyntax binaryExpression, SemanticModel model)
        {
            // Check if the binary expression is part of an argument
            if (binaryExpression.Ancestors().OfType<ArgumentSyntax>().Any() && !(Helper.IsBitwiseExpression(binaryExpression) || binaryExpression.IsKind(SyntaxKind.EqualsExpression)))
                return null;

            if (binaryExpression.IsKind(SyntaxKind.GreaterThanExpression)
                || binaryExpression.IsKind(SyntaxKind.GreaterThanOrEqualExpression)
                || binaryExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                var rightValue = model.GetConstantValue(binaryExpression.Right);
                if (rightValue.HasValue && Convert.ToInt32(rightValue.Value) == 0)
                {
                    return binaryExpression.Left;
                }
            }

            return binaryExpression;
        }

        /// <summary>
        /// Checks whether a variable or method is used in an expression
        /// </summary>
        /// <param name="expressionNode">The expression to analyze</param>
        /// <param name="enumVariableName">The identifier name of the enum</param>
        /// <returns>true, if a variable or method is used inside an expression</returns>
        private bool CheckForVariableInExpression(ExpressionSyntax expressionNode, SyntaxToken? enumVariableName)
        {
            string variableName = enumVariableName?.ValueText;
            var variableIdentifier = expressionNode.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText != enumVariableName?.ValueText).ToList();

            // Check if the enum variable is used multiple times in the expression without an AndAlso or an OrElse epxression to divide them
            if (expressionNode.DescendantNodes().OfType<IdentifierNameSyntax>().Count(node => node.Identifier.ValueText == variableName) > 1)
            {
                Logger.WriteWarning(string.Format("Variable \"{0}\" is used multiple times in the expression but is not divided by an && or an || expression. Following node will not be replaced: {1}", enumVariableName, expressionNode), 2, expressionNode);
                return true;
            }

            foreach (var identifier in variableIdentifier.ToList())
            {
                // Check if the identifier is part of a MemberAccessExpressionSyntax. If the MemberAccessExpressionSyntax contains the enumVariableName it can be filtered
                var memberAccessExpression = identifier.Ancestors().OfType<MemberAccessExpressionSyntax>().LastOrDefault();
                if (memberAccessExpression != null)
                {
                    if (Helper.GetAllIdentifiersByName(memberAccessExpression, variableName).Any())
                    {
                        variableIdentifier.Remove(identifier);
                        continue;
                    }
                }

                // Check if the identifier is part of a cast operation
                var castExpression = identifier.Ancestors().OfType<CastExpressionSyntax>().FirstOrDefault();
                if (castExpression?.Expression.DescendantNodesAndSelf().Contains(identifier) == false)
                {
                    variableIdentifier.Remove(identifier);
                    continue;
                }

                // Check if the identifier is used in a method which just converts the type
                var invocationExpression = identifier.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (invocationExpression != null)
                {
                    var invocationIdentifier = invocationExpression.Expression.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                    if (invocationIdentifier != null && Helper.IsConversionMethodAllowedInExpression(invocationIdentifier))
                    {
                        variableIdentifier.Remove(identifier);
                        continue;
                    }
                }

                // Check if the identifier is a static class
                var identifierType = _solution.GetTypeInfo(identifier).Type;
                if (identifierType?.TypeKind == TypeKind.Class && identifierType.IsStatic)
                {
                    variableIdentifier.Remove(identifier);
                    continue;
                }
            }

            if (variableIdentifier.Any())
            {
                Logger.WriteWarning(string.Format("Found identifier in expression named: {0}. Following node will not be replaced: {1}", variableIdentifier.First().ToString(), expressionNode), 2, expressionNode);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Replaces all literal expression (numerical expressions) inside the provided expression with the corresponding enum values.
        /// </summary>
        /// <param name="expressionSyntax">The epxression to replace</param>
        /// <param name="model">The semantic model</param>
        private void ReplaceNumericalExpressions(ExpressionSyntax expressionSyntax, SemanticModel model)
        {
            var literalExpressions = Helper.GetLiteralExpressionChildren(expressionSyntax);
            literalExpressions = Helper.FilterNumericalLiteralExpressions(literalExpressions);

            foreach (var literalExpression in literalExpressions)
            {
                int? numberValue = Helper.GetConstantNumberFromExpression(literalExpression, model);
                if (numberValue != null)
                {
                    ReplaceNodeWithEnumValues(literalExpression, (int)numberValue);
                }
            }
        }

        /// <summary>
        /// Checks, whether a bit is set more than once in the provided binary expression.
        /// </summary>
        /// <param name="binaryExpressionNode">The BinaryExpression to analyze</param>
        /// <param name="model">The semantic model</param>
        /// <returns>true, when a bit is set more than once</returns>
        private bool CheckForDuplicateBits(BinaryExpressionSyntax binaryExpressionNode, SemanticModel model)
        {
            if (binaryExpressionNode.IsKind(SyntaxKind.BitwiseOrExpression) || binaryExpressionNode.IsKind(SyntaxKind.AddExpression))
            {
                var numbers = binaryExpressionNode.DescendantNodes().OfType<LiteralExpressionSyntax>();
                int activeBits = 0;
                foreach (var number in numbers)
                {
                    var value = model.GetConstantValue(number);
                    if (value.HasValue && Helper.IsNumberOrChar(value.Value))
                    {
                        int bits = Convert.ToInt32(value.Value);

                        if ((activeBits & bits) > 0)
                            return true;

                        activeBits |= bits;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces a node with the corresponding enum values.
        /// </summary>
        /// <param name="node">The node to replace</param>
        /// <param name="value">The value for the enum replacement</param>
        /// <param name="canAddParentheses">Whether it is possible to add parenthesis surrounding the enum values</param>
        private void ReplaceNodeWithEnumValues(ExpressionSyntax node, int value, bool canAddParentheses = true)
        {
            var conversionInvocation = Helper.GetConversionMethodInvocation(node);
            var castExpression = Helper.GetCastExpression(node);
            var replaceNode = conversionInvocation ?? castExpression ?? node;

            // Check if a node has already been replaced
            if (_changedNodes.Contains(replaceNode))
            {
                Logger.WriteError(string.Format("The following node has already been replaced: {0}", node), 2, node);
                return;
            }

            string enumText = _enum.GetEntriesStringFromNumber(value, out int entryCount);

            if (canAddParentheses && entryCount > 1 && !(replaceNode.Parent is ParenthesizedExpressionSyntax))
                enumText = "(" + enumText + ")";

            var newNode = SyntaxFactory.ParseExpression(enumText);
            var trailingTrivia = replaceNode.GetTrailingTrivia();
            newNode = newNode.WithTrailingTrivia(trailingTrivia);

            _currEditor.ReplaceNode(replaceNode, newNode);

            _replacedNodeCount++;
            _changedNodes.Add(replaceNode);

            Logger.WriteLine(string.Format("Replaced node: {0} with: {1}", replaceNode, newNode), 2, node: replaceNode);
        }
    }
}
