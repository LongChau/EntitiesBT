using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EntitiesBT.Core;
using EntitiesBT.Variant;
using Unity.Entities;
using UnityEditor;
using UnityEditorInternal;

namespace EntitiesBT.Editor
{
    public class BlobArrayFieldCodeGenerator : INodeDataFieldCodeGenerator
    {
        public bool ShouldGenerate(FieldInfo fi)
        {
            return fi.FieldType.IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(BlobArray<>);
        }

        public string GenerateField(FieldInfo fi)
        {
            return $"public {fi.FieldType.GenericTypeArguments[0].FullName}[] {fi.Name};";
        }

        public string GenerateBuild(FieldInfo fi)
        {
            return $"builder.AllocateArray(ref data.{fi.Name}, {fi.Name});";
        }
    }

    public class BlobStringFieldCodeGenerator : INodeDataFieldCodeGenerator
    {
        public bool ShouldGenerate(FieldInfo fi)
        {
            return fi.FieldType.IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(BlobString);
        }

        public string GenerateField(FieldInfo fi)
        {
            return $"public string {fi.Name};";
        }

        public string GenerateBuild(FieldInfo fi)
        {
            return $"builder.AllocateString(ref data.{fi.Name}, {fi.Name});";
        }
    }

    public class BlobVariantFieldCodeGenerator : INodeDataFieldCodeGenerator
    {
        protected const string HEAD_LINE = "// Automatically generated by `BlobVariantFieldCodeGenerator`";

        public bool ShouldGenerateVariantInterface = true;
        public string VariantInterfaceDirectory = "Variant";
        public string VariantInterfaceNamespace = "EntitiesBT.Variant";
        public string VariantPropertyNameSuffix = "Variant";
        public NodeCodeGenerator Generator;
        public AssemblyDefinitionAsset Assembly;

        private ISet<Assembly> _referenceAssemblies;

        private bool IsReferenceType(Type type)
        {
            _referenceAssemblies ??= Assembly.ToAssembly().FindReferenceAssemblies();
            return _referenceAssemblies.Contains(type.Assembly);
        }

        public bool ShouldGenerate(FieldInfo fi)
        {
            if (!fi.FieldType.IsGenericType) return false;
            var variantType = fi.FieldType.GetGenericTypeDefinition();
            if (variantType == typeof(BlobVariantReader<>)) return true;
            if (variantType == typeof(BlobVariantWriter<>)) return true;
            if (variantType == typeof(BlobVariantReaderAndWriter<>)) return true;
            return false;
        }

        public string GenerateField(FieldInfo fi)
        {
            var valueType = fi.FieldType.GetGenericArguments()[0];
            var variantType = fi.FieldType.GetGenericTypeDefinition();
            var readerSuffix = $"{VariantPropertyNameSuffix}Reader";
            var writerSuffix = $"{VariantPropertyNameSuffix}Writer";
            var readerAndWriterSuffix = $"{VariantPropertyNameSuffix}ReaderAndWriter";
            if (ShouldGenerateVariantInterface)
            {
                if (variantType == typeof(BlobVariantReaderAndWriter<>))
                {
                    GenerateVariantInterface(valueType, readerSuffix, VariantGenerator.CreateReaderVariants);
                    GenerateVariantInterface(valueType, writerSuffix, VariantGenerator.CreateWriterVariants);
                    GenerateVariantInterface(valueType, readerAndWriterSuffix, (writer, valueType, isReferenceType, suffix) =>
                    {
                        writer.CreateReaderAndWriterVariants(valueType, isReferenceType, suffix);
                        writer.CreateSerializedReaderAndWriterVariant(valueType, suffix, $"{VariantPropertyNameSuffix}Reader", $"{VariantPropertyNameSuffix}Writer");
                    });
                }
                else if (variantType == typeof(BlobVariantReader<>))
                {
                    GenerateVariantInterface(valueType, readerSuffix, VariantGenerator.CreateReaderVariants);
                }
                else if (variantType == typeof(BlobVariantWriter<>))
                {
                    GenerateVariantInterface(valueType, writerSuffix, VariantGenerator.CreateWriterVariants);
                }
            }

            if (variantType == typeof(BlobVariantReaderAndWriter<>))
                return $"public {VariantInterfaceNamespace}.{valueType.Name}SerializedReaderAndWriterVariant {fi.Name};";

            var suffix = variantType == typeof(BlobVariantReader<>) ? readerSuffix : writerSuffix;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("[UnityEngine.SerializeReference, SerializeReferenceButton]");
            stringBuilder.Append(" ");
            stringBuilder.AppendLine($"public {VariantInterfaceNamespace}.{valueType.Name}{suffix} {fi.Name};");
            return stringBuilder.ToString();
        }

        public string GenerateBuild(FieldInfo fi)
        {
            return $"{fi.Name}.Allocate(ref builder, ref data.{fi.Name}, Self, tree);";
        }

        private void GenerateVariantInterface(Type valueType, string suffix, CreateVariants createVariants)
        {
            var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Generator)) + "/" + VariantInterfaceDirectory;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var filepath = $"{directory}/{valueType.Name}{suffix}.cs";
            if (!File.Exists(filepath) || File.ReadLines(filepath).FirstOrDefault() == HEAD_LINE)
            {
                using var writer = new StreamWriter(filepath);
                writer.WriteLine(HEAD_LINE);
                writer.WriteLine(VariantGenerator.NamespaceBegin(VariantInterfaceNamespace));
                createVariants(writer, valueType, IsReferenceType, suffix);
                writer.WriteLine(VariantGenerator.NamespaceEnd());
            }
        }

        private delegate void CreateVariants(StreamWriter writer, Type valueType, Predicate<Type> isReferenceType, string suffix);
    }

    public class BlobVariantFieldCodeGeneratorForOdin : INodeDataFieldCodeGenerator
    {
        public bool ShouldGenerate(FieldInfo fi)
        {
            return fi.FieldType.IsGenericType && fi.FieldType.GetGenericTypeDefinition() == typeof(BlobVariantReader<>);
        }

        public string GenerateField(FieldInfo fi)
        {
            var variantType = fi.FieldType.GetGenericArguments()[0];
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("[OdinSerialize, NonSerialized]");
            stringBuilder.Append("        ");
            stringBuilder.AppendLine($"public EntitiesBT.Variant.VariantProperty<{variantType.FullName}> {fi.Name};");
            return stringBuilder.ToString();
        }

        public string GenerateBuild(FieldInfo fi)
        {
            return $"{fi.Name}.Allocate(ref builder, ref data.{fi.Name}, Self, tree);";
        }
    }
}
