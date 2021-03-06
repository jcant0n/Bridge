using System;
using Bridge.Contract;
using Bridge.Contract.Constants;

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bridge.Translator
{
    public partial class ConstructorBlock : AbstractMethodBlock, IConstructorBlock
    {
        public ConstructorBlock(IEmitter emitter, ITypeInfo typeInfo, bool staticBlock)
            : base(emitter, typeInfo.TypeDeclaration)
        {
            this.Emitter = emitter;
            this.TypeInfo = typeInfo;
            this.StaticBlock = staticBlock;
        }

        public ITypeInfo TypeInfo
        {
            get;
            set;
        }

        public bool StaticBlock
        {
            get;
            set;
        }

        public bool HasEntryPoint
        {
            get; set;
        }

        protected override void DoEmit()
        {
            if (this.StaticBlock)
            {
                this.EmitCtorForStaticClass();
            }
            else
            {
                this.EmitCtorForInstantiableClass();
            }
        }

        protected virtual IEnumerable<string> GetInjectors()
        {
            var handlers = this.GetEventsAndAutoStartupMethods();
            var injectors = this.Emitter.Plugins.GetConstructorInjectors(this);
            return injectors.Concat(handlers);
        }

        protected virtual void EmitCtorForStaticClass()
        {
            var ctor = this.TypeInfo.StaticCtor;

            if (ctor != null && ctor.Body.HasChildren)
            {
                this.ResetLocals();
                var prevNamesMap = this.BuildLocalsNamesMap();
                this.Write(JS.Funcs.CONSTRUCTOR);
                this.WriteColon();
                this.WriteFunction();
                this.WriteOpenCloseParentheses(true);

                this.BeginBlock();
                var beginPosition = this.Emitter.Output.Length;

                ctor.Body.AcceptChildren(this.Emitter);

                if (!this.Emitter.IsAsync)
                {
                    this.EmitTempVars(beginPosition, true);
                }

                this.EndBlock();
                this.ClearLocalsNamesMap(prevNamesMap);
                this.Emitter.Comma = true;
            }

            var injectors = this.GetInjectors();
            IEnumerable<string> fieldsInjectors = null;

            var fieldBlock = new FieldBlock(this.Emitter, this.TypeInfo, true, true);
            fieldBlock.Emit();

            fieldsInjectors = fieldBlock.Injectors;

            if (fieldBlock.WasEmitted)
            {
                this.Emitter.Comma = true;
            }

            if (this.TypeInfo.StaticConfig.HasConfigMembers || injectors.Any() || fieldsInjectors.Any())
            {
                this.EnsureComma();

                if (this.TypeInfo.StaticMethods.Any(m => m.Value.Any(subm => this.Emitter.GetEntityName(subm) == JS.Fields.CONFIG)) ||
                    this.TypeInfo.StaticConfig.Fields.Any(m => m.GetName(this.Emitter) == JS.Fields.CONFIG))
                {
                    this.Write(JS.Vars.D);
                }

                this.Write(JS.Fields.CONFIG);

                this.WriteColon();
                this.BeginBlock();

                if (this.TypeInfo.StaticConfig.HasConfigMembers)
                {
                    var configBlock = new FieldBlock(this.Emitter, this.TypeInfo, true, false);
                    configBlock.Emit();

                    if (configBlock.Injectors.Count > 0)
                    {
                        injectors = configBlock.Injectors.Concat(injectors);
                    }

                    if (configBlock.WasEmitted)
                    {
                        this.Emitter.Comma = true;
                    }
                }

                if (fieldsInjectors.Any())
                {
                    injectors = fieldsInjectors.Concat(injectors);
                }

                if (injectors.Count() > 0)
                {
                    this.EnsureComma();

                    this.Write(JS.Funcs.INIT);
                    this.WriteColon();
                    this.WriteFunction();
                    this.WriteOpenParentheses();
                    this.WriteCloseParentheses();
                    this.WriteSpace();
                    this.BeginBlock();
                    foreach (var fn in injectors)
                    {
                        this.Write(fn);
                        this.WriteNewLine();
                    }
                    this.EndBlock();
                    this.Emitter.Comma = true;
                }
                this.WriteNewLine();
                this.EndBlock();
            }
        }

        protected virtual IEnumerable<string> EmitInitMembers()
        {
            var injectors = this.GetInjectors();

            var constructorWrapperString = CS.Wrappers.CONSTRUCTORWRAPPER + ":";

            IEnumerable<string> ctorWrappers = injectors.Where(i => i.StartsWith(constructorWrapperString)).Select(i => i.Substring(constructorWrapperString.Length));
            injectors = injectors.Where(i => !i.StartsWith(constructorWrapperString));

            IEnumerable<string> fieldsInjectors = null;

            var fieldBlock = new FieldBlock(this.Emitter, this.TypeInfo, false, true);
            fieldBlock.Emit();

            fieldsInjectors = fieldBlock.Injectors;

            if (fieldBlock.WasEmitted)
            {
                this.Emitter.Comma = true;
            }

            if (!this.TypeInfo.InstanceConfig.HasConfigMembers && !injectors.Any() && !fieldsInjectors.Any())
            {
                return ctorWrappers;
            }

            int pos = this.Emitter.Output.Length;
            bool oldComma = this.Emitter.Comma;
            bool oldNewLine = this.Emitter.IsNewLine;

            if (this.TypeInfo.InstanceMethods.Any(m => m.Value.Any(subm => this.Emitter.GetEntityName(subm) == JS.Fields.CONFIG)) ||
                this.TypeInfo.InstanceConfig.Fields.Any(m => m.GetName(this.Emitter) == JS.Fields.CONFIG))
            {
                this.Write(JS.Vars.D);
            }

            this.EnsureComma();
            this.Write(JS.Fields.CONFIG);

            this.WriteColon();
            this.BeginBlock();

            if (this.TypeInfo.InstanceConfig.HasConfigMembers)
            {
                var configBlock = new FieldBlock(this.Emitter, this.TypeInfo, false, false);
                configBlock.Emit();

                if (configBlock.Injectors.Count > 0)
                {
                    injectors = configBlock.Injectors.Concat(injectors);
                }

                if (configBlock.WasEmitted)
                {
                    this.Emitter.Comma = true;
                }
            }

            if (fieldsInjectors.Any())
            {
                injectors = fieldsInjectors.Concat(injectors);
            }

            if (injectors.Count() > 0)
            {
                this.EnsureComma();
                this.Write(JS.Funcs.INIT);
                this.WriteColon();
                this.WriteFunction();
                this.WriteOpenParentheses();
                this.WriteCloseParentheses();
                this.WriteSpace();
                this.BeginBlock();

                foreach (var fn in injectors)
                {
                    this.Write(fn);
                    this.WriteNewLine();
                }

                this.EndBlock();

                this.Emitter.Comma = true;
            }

            this.WriteNewLine();
            this.EndBlock();

            var str = this.Emitter.Output.ToString().Substring(pos);

            if (Regex.IsMatch(str, "^\\s*\\$?" + JS.Fields.CONFIG + "\\s*:\\s*\\{\\s*\\}\\s*$", RegexOptions.Multiline))
            {
                this.Emitter.Output.Length = pos;
                this.Emitter.Comma = oldComma;
                this.Emitter.IsNewLine = oldNewLine;
            }

            return ctorWrappers;
        }

        protected virtual void EmitCtorForInstantiableClass()
        {
            var ctorWrappers = this.EmitInitMembers().ToArray();

            if (!this.TypeInfo.HasInstantiable && ctorWrappers.Length == 0)
            {
                return;
            }

            var baseType = this.Emitter.GetBaseTypeDefinition();
            var typeDef = this.Emitter.GetTypeDefinition();

            if (typeDef.IsValueType || (this.TypeInfo.Ctors.Count == 0 && ctorWrappers.Length > 0))
            {
                this.TypeInfo.Ctors.Add(new ConstructorDeclaration
                {
                    Modifiers = Modifiers.Public,
                    Body = new BlockStatement()
                });
            }

            foreach (var ctor in this.TypeInfo.Ctors)
            {
                this.EnsureComma();
                this.ResetLocals();
                var prevMap = this.BuildLocalsMap();
                var prevNamesMap = this.BuildLocalsNamesMap();
                this.AddLocals(ctor.Parameters, ctor.Body);

                var ctorName = JS.Funcs.CONSTRUCTOR;

                if (this.TypeInfo.Ctors.Count > 1 && ctor.Parameters.Count > 0)
                {
                    var overloads = OverloadsCollection.Create(this.Emitter, ctor);
                    ctorName = overloads.GetOverloadName();
                }

                XmlToJsDoc.EmitComment(this, ctor);
                this.Write(ctorName);

                this.WriteColon();
                this.WriteFunction();

                int pos = this.Emitter.Output.Length;
                this.EmitMethodParameters(ctor.Parameters, null, ctor);
                var ctorParams = this.Emitter.Output.ToString().Substring(pos);

                this.WriteSpace();
                this.BeginBlock();
                var len = this.Emitter.Output.Length;
                var requireNewLine = false;

                var noThisInvocation = ctor.Initializer == null || ctor.Initializer.IsNull || ctor.Initializer.ConstructorInitializerType == ConstructorInitializerType.Base;
                IWriterInfo oldWriter = null;
                if (ctorWrappers.Length > 0 && noThisInvocation)
                {
                    oldWriter = this.SaveWriter();
                    this.NewWriter();
                }

                this.ConvertParamsToReferences(ctor.Parameters);

                if (len != this.Emitter.Output.Length)
                {
                    requireNewLine = true;
                }

                if (noThisInvocation)
                {
                    if (requireNewLine)
                    {
                        this.WriteNewLine();
                    }
                    this.Write("this." + JS.Funcs.INITIALIZE + "();");
                    requireNewLine = true;
                }

                if (baseType != null && (!this.Emitter.Validator.IsIgnoreType(baseType) || this.Emitter.Validator.IsBridgeClass(baseType)) ||
                    (ctor.Initializer != null && ctor.Initializer.ConstructorInitializerType == ConstructorInitializerType.This))
                {
                    if (requireNewLine)
                    {
                        this.WriteNewLine();
                    }
                    this.EmitBaseConstructor(ctor, ctorName);
                    requireNewLine = true;
                }

                var script = this.Emitter.GetScript(ctor);

                if (script == null)
                {
                    if (ctor.Body.HasChildren)
                    {
                        var beginPosition = this.Emitter.Output.Length;
                        if (requireNewLine)
                        {
                            this.WriteNewLine();
                        }

                        ctor.Body.AcceptChildren(this.Emitter);

                        if (!this.Emitter.IsAsync)
                        {
                            this.EmitTempVars(beginPosition, true);
                        }
                    }
                    else if (requireNewLine)
                    {
                        this.WriteNewLine();
                    }
                }
                else
                {
                    if (requireNewLine)
                    {
                        this.WriteNewLine();
                    }

                    foreach (var line in script)
                    {
                        this.Write(line);
                        this.WriteNewLine();
                    }
                }

                if (oldWriter != null)
                {
                    this.WrapBody(oldWriter, ctorWrappers, ctorParams);
                }

                this.EndBlock();
                this.Emitter.Comma = true;
                this.ClearLocalsMap(prevMap);
                this.ClearLocalsNamesMap(prevNamesMap);
            }
        }

        protected virtual void WrapBody(IWriterInfo oldWriter, string[] ctorWrappers, string ctorParams)
        {
            var body = this.Emitter.Output.ToString();
            this.RestoreWriter(oldWriter);

            List<string> endParts = new List<string>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ctorWrappers.Length; i++)
            {
                var isLast = i == (ctorWrappers.Length - 1);
                var ctorWrapper = ctorWrappers[i];
                var parts = ctorWrapper.Split(new[] { CS.Wrappers.Params.BODY }, StringSplitOptions.RemoveEmptyEntries);
                endParts.Add(parts[1]);

                sb.Append(parts[0]);
                sb.Append("function ");
                sb.Append(ctorParams);
                sb.Append(" {");

                if (!isLast)
                {
                    sb.Append("\n");
                }

                ++this.Emitter.Level;
                for (var j = 0; j < this.Emitter.Level; j++)
                {
                    sb.Append("    ");
                }

                if (isLast)
                {
                    sb.Append(this.WriteIndentToString(body));
                }
            }

            endParts.Reverse();

            var newLine = false;
            foreach (var endPart in endParts)
            {
                --this.Emitter.Level;
                if (newLine)
                {
                    sb.Append("\n");
                    for (var j = 0; j < this.Emitter.Level; j++)
                    {
                        sb.Append("    ");
                    }
                }
                else if(sb.ToString().Substring(sb.Length - 4) == "    ")
                {
                    sb.Length -= 4;
                }
                newLine = true;

                sb.Append("}");
                sb.Append(endPart);
            }

            this.Write(sb.ToString());
            this.WriteNewLine();
        }

        protected virtual void EmitBaseConstructor(ConstructorDeclaration ctor, string ctorName)
        {
            var initializer = ctor.Initializer != null && !ctor.Initializer.IsNull ? ctor.Initializer : new ConstructorInitializer()
            {
                ConstructorInitializerType = ConstructorInitializerType.Base
            };

            bool appendScope = false;

            if (initializer.ConstructorInitializerType == ConstructorInitializerType.Base)
            {
                var baseType = this.Emitter.GetBaseTypeDefinition();
                var baseName = JS.Funcs.CONSTRUCTOR;
                if (ctor.Initializer != null && !ctor.Initializer.IsNull)
                {
                    var member = ((InvocationResolveResult)this.Emitter.Resolver.ResolveNode(ctor.Initializer, this.Emitter)).Member;
                    var overloads = OverloadsCollection.Create(this.Emitter, member);
                    if (overloads.HasOverloads)
                    {
                        baseName = overloads.GetOverloadName();
                    }
                }

                if (baseName == JS.Funcs.CONSTRUCTOR)
                {
                    baseName = JS.Funcs.DCONSTRUCTOR;
                }

                string name = null;

                if (this.TypeInfo.GetBaseTypes(this.Emitter).Any())
                {
                    name = BridgeTypes.ToJsName(this.TypeInfo.GetBaseClass(this.Emitter), this.Emitter);
                }
                else
                {
                    name = BridgeTypes.ToJsName(baseType, this.Emitter);
                }

                this.Write(name, ".");
                this.Write(baseName);
                this.WriteCall();
                appendScope = true;
            }
            else
            {
                // this.WriteThis();
                string name = BridgeTypes.ToJsName(this.TypeInfo.Type, this.Emitter);
                this.Write(name);
                this.WriteDot();

                var baseName = JS.Funcs.CONSTRUCTOR;
                var member = ((InvocationResolveResult)this.Emitter.Resolver.ResolveNode(ctor.Initializer, this.Emitter)).Member;
                var overloads = OverloadsCollection.Create(this.Emitter, member);
                if (overloads.HasOverloads)
                {
                    baseName = overloads.GetOverloadName();
                }

                if (baseName == JS.Funcs.CONSTRUCTOR)
                {
                    baseName = JS.Funcs.DCONSTRUCTOR;
                }

                this.Write(baseName);
                this.WriteCall();
                appendScope = true;
            }

            this.WriteOpenParentheses();

            if (appendScope)
            {
                this.WriteThis();

                if (initializer.Arguments.Count > 0)
                {
                    this.WriteComma();
                }
            }

            if (initializer.Arguments.Count > 0)
            {
                var argsInfo = new ArgumentsInfo(this.Emitter, ctor.Initializer);
                var argsExpressions = argsInfo.ArgumentsExpressions;
                var paramsArg = argsInfo.ParamsExpression;

                new ExpressionListBlock(this.Emitter, argsExpressions, paramsArg, ctor.Initializer).Emit();    
            }

            this.WriteCloseParentheses();
            this.WriteSemiColon();
            this.WriteNewLine();
        }

        protected virtual bool IsGenericType()
        {
            return this.TypeInfo.Type.TypeParameterCount > 0;
        }

        private bool IsGenericMethod(MethodDeclaration methodDeclaration)
        {
            return methodDeclaration.TypeParameters.Any();
        }
    }
}
