﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace LanguageExt;

public static class TypeInfoAllMemberExtensions
{
    public static IEnumerable<ConstructorInfo> GetAllConstructors(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredConstructors, includeBase);

    public static IEnumerable<EventInfo> GetAllEvents(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredEvents, includeBase);

    public static IEnumerable<FieldInfo> GetAllFields(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredFields, includeBase);

    public static IEnumerable<MemberInfo> GetAllMembers(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredMembers, includeBase);

    public static IEnumerable<MethodInfo> GetAllMethods(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredMethods, includeBase);

    public static IEnumerable<TypeInfo> GetAllNestedTypes(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredNestedTypes, includeBase);

    public static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo typeInfo, bool includeBase) => 
        GetAll(typeInfo, ti => ti.DeclaredProperties, includeBase);

    private static IEnumerable<T> GetAll<T>(TypeInfo typeInfo, Func<TypeInfo, IEnumerable<T>> accessor, bool includeBase)
    {
        if (includeBase)
        {
            var ti = typeInfo; 
            while (ti != null)
            {
                foreach (var t in accessor(ti))
                {
                    yield return t;
                }
                ti = ti.BaseType?.GetTypeInfo();
            }
        }
        else
        {
            foreach (var t in accessor(typeInfo))
            {
                yield return t;
            }
        }
    }
}
