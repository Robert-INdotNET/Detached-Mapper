﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using static Detached.RuntimeTypes.Expressions.ExtendedExpression;
using static System.Linq.Expressions.Expression;

namespace Detached.Mappers.TypeOptions.Dictionary
{
    public class DictionaryTypeOptions : ITypeOptions
    {
        public Dictionary<string, object> Annotations { get; set; }

        public bool IsCollection => false;

        public bool IsEntity => false;

        public bool IsFragment => false;

        public bool IsComplex => true;

        public bool IsPrimitive => false;

        public bool IsAbstract => false;

        public bool IsInherited => false;

        public Type ItemClrType => null;

        public IEnumerable<string> MemberNames => new string[0];

        public Type ClrType => typeof(Dictionary<string, object>);

        public bool UsePatchProxy => false;

        public string DiscriminatorName => null;

        public Dictionary<object, Type> DiscriminatorValues => null;

        public bool IsNullable => false;

        public Expression BuildIsSetExpression(Expression instance, Expression context, string memberName)
        {
            return Call("ContainsKey", instance, Constant(memberName));
        }

        public Expression BuildNewExpression(Expression context, Expression discriminator)
        {
            return New(typeof(Dictionary<string, object>));
        }

        public IMemberOptions GetMember(string memberName)
        {
            return new DictionaryMemberOptions(memberName);
        }
    }
}
