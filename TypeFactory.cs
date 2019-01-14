using System;

using System.Collections.Generic;

using System.Reflection;

using System.Reflection.Emit;

using System.Xml.Serialization;


namespace MirLoaderClasses.Utils

{

    public class TypeFactory

    {

        public static Type CreateType(TypeConfig config)

        {

            return CompileType(config.Name, config.FieldConfigs, config.XmlName, config.AttributeSerialized);

        }


        public static object CreateNewObject(Type type)

        {

            return Activator.CreateInstance(type);

        }


        public static Type CompileType(string typeName, IEnumerable<FieldConfig> yourListOfFields, string xmlName, bool isAttrSerialized)

        {

            var tb = GetTypeBuilder(typeName, xmlName);

            foreach (var field in yourListOfFields)

                CreateProperty(tb, field.Name, field.FieldType, isAttrSerialized);


            var objectType = tb.CreateType();

            return objectType;

        }


        private static TypeBuilder GetTypeBuilder(string typeSignature, string xmlElemName)

        {

            var an = new AssemblyName(typeSignature);

            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            var tb = moduleBuilder.DefineType(typeSignature

                                , TypeAttributes.Public |

                                TypeAttributes.Class |

                                TypeAttributes.AutoClass |

                                TypeAttributes.AnsiClass |

                                TypeAttributes.BeforeFieldInit |

                                TypeAttributes.AutoLayout

                                , null);

            var constructorInfo = typeof(XmlRootAttribute).GetConstructor(new[] { typeof(string) });

            if (constructorInfo == null) return tb;

            var attrBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { xmlElemName });

            tb.SetCustomAttribute(attrBuilder);

            return tb;

        }


        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType, bool isAttrSerialized)

        {

            var fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            var propertyBuilder = tb.DefineProperty(propertyName, (PropertyAttributes)CallingConventions.Standard, propertyType, null);

            var getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);

            var getIl = getPropMthdBldr.GetILGenerator();


            getIl.Emit(OpCodes.Ldarg_0);

            getIl.Emit(OpCodes.Ldfld, fieldBuilder);

            getIl.Emit(OpCodes.Ret);


            MethodBuilder setPropMthdBldr =

                tb.DefineMethod("set_" + propertyName,

                  MethodAttributes.Public |

                  MethodAttributes.SpecialName |

                  MethodAttributes.HideBySig,

                  null, new[] { propertyType });


            var setIl = setPropMthdBldr.GetILGenerator();

            var modifyProperty = setIl.DefineLabel();

            var exitSet = setIl.DefineLabel();


            setIl.MarkLabel(modifyProperty);

            setIl.Emit(OpCodes.Ldarg_0);

            setIl.Emit(OpCodes.Ldarg_1);

            setIl.Emit(OpCodes.Stfld, fieldBuilder);


            setIl.Emit(OpCodes.Nop);

            setIl.MarkLabel(exitSet);

            setIl.Emit(OpCodes.Ret);


            propertyBuilder.SetGetMethod(getPropMthdBldr);

            propertyBuilder.SetSetMethod(setPropMthdBldr);


 

            if (!isAttrSerialized || propertyType.IsArray) return;

            var constructorInfo = typeof(XmlAttributeAttribute).GetConstructor(new[] { typeof(string) });

            if (constructorInfo == null) return;

            var attrBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { propertyName });

            propertyBuilder.SetCustomAttribute(attrBuilder);

        }

    }

}

 
