﻿using Backender.CodeEditor.CSharp;
using Backender.CodeEditor.CSharp.Objects;
using Backender.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backender.CodeGenerator.Patterns.Repo
{
	public static class DtoHandler
	{
		public static Class DtoGenerate(this Entity entity, ref Project proj, List<string> Options = null)
		{
			var options = new List<string>();
			options.Add("DtoClass");
			if (Options != null)
			{
				options.AddRange(Options);
			}
			var entityClass = proj.AddClass(entity.EntityName + "Dto", baseClassName: "BaseDto", Options: options, AppendNameSpace: "Dto");
			foreach (var Col in entity.Cols)
			{
				entityClass.AddProperty(Col.ColType, Col.ColName, AccessModifier.Public);
			}

			//Add Realations
			return entityClass;
		}
		public static void AddDtoRealations(this Project proj, ref List<RealationShip> realationShips)
		{
			var options = new List<string>();
			foreach (var Realation in realationShips)
			{
				var class1 = proj.GetClassByName(Realation.Entity1 + "Dto");
				var class2 = proj.GetClassByName(Realation.Entity2 + "Dto");
				if (class1 == null || class2 == null) continue;
				switch (Realation.RealationShipType)
				{
					case "M2M":
						break;
					case "O2M":
						class2.AddProperty(Realation.Entity1 + "Dto", Realation.Entity1 + "Dto", AccessModifier.Public);
						break;
					case "O2O":
						class2.AddProperty(Realation.Entity1 + "Dto", Realation.Entity1 + "Dto", AccessModifier.Public);
						break;
					default:
						break;
				}
			}
		}
		public static List<Class> FactoriesGenerate(this List<Entity> entities, ref Project proj, Project coreProj)
		{
			//entities = entities.OrderBy(p => p.EntityCategory).ToList();
			var FactoryClasses = new List<Class>();
			foreach (var entity in entities)
			{
				if (entity.EntityCategory == null)
				{
					entity.EntityCategory = string.Empty;
				}
				var entityFactoryClassName = entity.EntityCategory + "DtosFactory";
				var entityFactory = FactoryClasses.FirstOrDefault(p => p.Name == entityFactoryClassName);

				if (!FactoryClasses.Any(p => p.Name == entityFactoryClassName))
				{
					entityFactory = proj.AddClass(entityFactoryClassName, AppendNameSpace: entity.EntityCategory);
					entityFactory.UsingNameSpaces.Add(proj.SolutionName + ".Core.Domains");
					if (!string.IsNullOrEmpty(entity.EntityCategory))
					{
						entityFactory.UsingNameSpaces.Add(proj.SolutionName + ".Core.Domains." + entity.EntityCategory);
					}
				}
				AddPrepareMethod(entityFactory, entity, coreProj);
				AddPrepareMethodOverLoad(entityFactory, entity, coreProj);
				FactoryClasses.Add(entityFactory);
			}
			foreach (var FactoryClass in FactoryClasses)
			{
				proj.AddClass(FactoryClass);
			}

			return FactoryClasses;
		}
		private static void AddPrepareMethod(Class entityFactory, Entity entity, Project coreProj)
		{
			var Parameter = new MethodParameter()
			{
				DataType = entity.EntityName,
				Name = entity.EntityName.ToLower()
			};
			var DtoClass = coreProj.GetClassByName(entity.EntityName + "Dto");

			var DtoClassValues = new List<string>();
			var DtoDtosProperties = new List<Property>();
			foreach (var dtoProperty in DtoClass.InnerItems.OfType<Property>())
			{
				if (coreProj.IsClassExist(dtoProperty.DataType)) {
					DtoDtosProperties.Add(dtoProperty);
					continue; 
				}
				DtoClassValues.Add($"{dtoProperty.Name} = {Parameter.Name}.{dtoProperty.Name}");
			}
			var PrepareCode = $"var {entity.EntityName.ToLower()}Dto = new {DtoClass.Name}()\n" +
			"{\n" + string.Join(",\n", DtoClassValues) + $"\n}};\n";
			foreach (var DtoDtosProperty in DtoDtosProperties)
			{
				var entityName = DtoDtosProperty.Name.Substring(0, DtoDtosProperty.Name.Length-3);
				var idParameter = "";
				if (entityName == Parameter.DataType)
				{
					idParameter = $"{Parameter.Name}.Id";
				}
				else
				{
					idParameter = $"{Parameter.Name}.{entityName}Id";
				}
				PrepareCode += $"\n{entity.EntityName.ToLower()}Dto.{entityName} = Prepare{entityName}Dto(_{entityName.ToLower()}Service.Get{entityName}ById({idParameter}));";
			}
			PrepareCode += $"\nreturn {entity.EntityName.ToLower()}Dto;";
			entityFactory.AddMethod(entity.EntityName + "Dto",
				$"Prepare{entity.EntityName}Dto",
				PrepareCode,
				Parameter);
		}
		private static void AddPrepareMethodOverLoad(Class entityFactory, Entity entity, Project coreProj)
		{
			var Parameter = new MethodParameter()
			{
				DataType = $"List<{entity.EntityName}>",
				Name = entity.EntityName.ToLower().ToPlural()
			};

			var PrepareCode = $"var {entity.EntityName.ToLower()}Dtos = new List<{entity.EntityName}Dto>();\n" +
				$"foreach (var {entity.EntityName.ToLower()} in {Parameter.Name})\n" +
				$"{{\n" +
				$"{entity.EntityName.ToLower()}Dtos.Add(Prepare{entity.EntityName}Dto(entity.EntityName.ToLower()));\n" +
				$"}}\n" +
				$"return {entity.EntityName.ToLower()}Dtos;";
			entityFactory.AddMethod($"List<{entity.EntityName}Dto>",
				$"Prepare{entity.EntityName}Dto",
				PrepareCode,
				Parameter);
		}
		public static Class FactoriyGenerate(this Entity entity, ref Project proj)
		{
			throw new NotImplementedException();
		}

	}
}
