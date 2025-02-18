﻿using Detached.Mappers.Annotations;
using Detached.Mappers.Types;
using Detached.Mappers.Types.Class;
using Detached.Mappers.Types.Conventions;
using Detached.PatchTypes;
using Detached.RuntimeTypes.Reflection;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Detached.RuntimeTypes.Expressions.ExtendedExpression;
using static System.Linq.Expressions.Expression;

namespace Detached.Mappers.Types.Class
{
    public class ClassTypeFactory : ITypeFactory
    {
        public IType Create(MapperOptions options, Type type)
        {
            ClassType typeOptions = new ClassType();
            typeOptions.ClrType = type;
            typeOptions.IsAbstract = type == typeof(object) || type.IsAbstract || type.IsInterface;

            if (options.IsPrimitive(type))
            {
                typeOptions.MappingSchema = MappingSchema.Primitive;
            }
            else if (type.IsEnumerable(out Type itemType))
            {
                typeOptions.ItemClrType = itemType;
                typeOptions.MappingSchema = MappingSchema.Collection;
            }
            else if (type.IsNullable(out Type baseType))
            {
                typeOptions.MappingSchema = MappingSchema.Nullable;
                typeOptions.ItemClrType = baseType;
            }
            else
            {
                typeOptions.MappingSchema = MappingSchema.Complex;

                bool canTryGet = typeof(IPatch).IsAssignableFrom(type);

                // generate members.
                foreach (PropertyInfo propInfo in type.GetRuntimeProperties())
                {
                    if (ShouldMap(propInfo))
                    {
                        ClassTypeMember memberOptions = new ClassTypeMember();
                        memberOptions.Name = propInfo.Name;
                        memberOptions.ClrType = propInfo.PropertyType;
                        memberOptions.PropertyInfo = propInfo;
                        memberOptions.CanTryGet = canTryGet;

                        // generate getter.
                        if (propInfo.CanRead)
                        {
                            memberOptions.Getter = Lambda(
                                    Parameter(type, out Expression instanceExpr),
                                    Property(instanceExpr, propInfo)
                                );
                        }

                        // generate setter.
                        if (propInfo.CanWrite)
                        {
                            memberOptions.Setter = Lambda(
                                   Parameter(type, out Expression instanceExpr),
                                   Parameter(propInfo.PropertyType, out Expression valueExpr),
                                   Assign(Property(instanceExpr, propInfo), valueExpr)
                               );
                        }

                        // apply member attributes.
                        foreach (Attribute annotation in propInfo.GetCustomAttributes())
                        {
                            if (options.AnnotationHandlers.TryGetValue(annotation.GetType(), out IAnnotationHandler handler))
                            {
                                handler.Apply(annotation, options, typeOptions, memberOptions);
                            }
                        }

                        typeOptions.Members.Add(memberOptions);
                    }
                }
            }

            ConstructorInfo constructorInfo = typeOptions.ClrType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (!typeOptions.IsAbstract && constructorInfo != null)
            {
                typeOptions.Constructor = Lambda(New(constructorInfo));
            }

            // apply type attributes.
            foreach (Attribute annotation in type.GetCustomAttributes())
            {
                if (options.AnnotationHandlers.TryGetValue(annotation.GetType(), out IAnnotationHandler handler))
                {
                    handler.Apply(annotation, options, typeOptions, null);
                }
            }

            // apply conventions.
            foreach (ITypeConvention convention in options.TypeConventions)
            {
                convention.Apply(options, typeOptions);
            }

            // manual configuration is applied after all of this.
            return typeOptions;
        }

        public virtual bool ShouldMap(PropertyInfo propInfo)
        {
            bool result = true;

            if (propInfo.Name == "Modified"
                && typeof(IPatch).IsAssignableFrom(propInfo.DeclaringType))
            {
                result = false;
            }

            return result;
        }
    }
}