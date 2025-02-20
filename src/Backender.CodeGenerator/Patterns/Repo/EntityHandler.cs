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
	/// <summary>
    /// This class Generate the entity classes with their options
	/// </summary>
	public static class EntityHandler
    {
		/// <summary>
		/// Create Entity Class and add properties based on entity section of config informations.
		/// </summary>
		/// <param name="entity">The entity section of config informations.</param>
		/// <param name="proj">The project in which the entity class is built.</param>
		/// <param name="Options">Some Options for the Created Class.</param>
		/// <returns>
		/// The class that was created.
		/// </returns>
		public static Class EnitityGenerate(this Entity entity,ref Project proj,List<string> Options =null)
        {
			var AppendNameSpace = "Domains";

			if (!string.IsNullOrEmpty(entity.EntityCategory))
			{
				AppendNameSpace += "." + entity.EntityCategory;
			}

			var options = new List<string>();
            options.Add("EntityClass");
            if (Options != null)
            {
                options.AddRange(Options);
            }
            var entityClass = proj.AddClass(entity.EntityName, baseClassName: "BaseEntity", Options: options,AppendNameSpace: AppendNameSpace);
            foreach (var Col in entity.Cols)
            {
               var propery= entityClass.AddProperty(Col.ColType, Col.ColName, AccessModifier.Public);
				foreach (var option in Col.Options.Split(' '))
				{
					switch (option)
					{
						case "-r":
							propery.AddAttributeToProperty("Required").AddRequiredNameSpaces("System.ComponentModel.DataAnnotations");
							break;
						default:
							break;
					}
				}
			}
			entityClass.UsingNameSpaces.Add(proj.DefaultNameSpace + ".Enums");
			//Add relations
			return entityClass;
        }

		/// <summary>
		/// Adding the relationShips based on entity pasectionrt of config informations to Entity Classes
		/// </summary>
		/// <param name="proj">The project in which the entity class is built.</param>
		/// <param name="relationShips">The relation section of config informations.</param>
		public static void Addrelations(this Project proj, ref List<RelationShip> relationShips)
        {
            var options = new List<string>();
            var Appendrelations = new List<RelationShip>();
            foreach (var relation in relationShips)
            {
                var class1 = proj.GetClassByName(relation.Entity1);
                var class2 = proj.GetClassByName(relation.Entity2);
                switch (relation.RelationShipType)
                {
                    case "M2M":
                        var MiddleClassDomain = CreateMiddleClass(relation.Entity1, relation.Entity2);
                        var MiddleClassEntity = MiddleClassDomain.Entites.FirstOrDefault();
                        options.Clear();
                        options.Add("MiddleClass");

                        var MiddleClass = MiddleClassEntity.EnitityGenerate(ref proj, options);
                        Appendrelations.AddRange(MiddleClassDomain.RelationShips);
                        //var MiddleClassName = class1.Name + class2.Name;
                        //var MiddleClass = proj.AddClass(MiddleClassName);
                        //MiddleClass.AddProperty(class1.Name, class1.Name + "Id");
                        //MiddleClass.AddProperty(class2.Name, class2.Name + "Id");
                        //MiddleClass.AddProperty(class1.Name, class1.Name, isVirtual: true);
                        //MiddleClass.AddProperty(class2.Name, class2.Name, isVirtual: true);
                        //class1.AddProperty($"IEnumerable<{MiddleClassName}>", MiddleClassName.ToPlural(), isVirtual: true);
                        //class1.AddProperty($"IEnumerable<{MiddleClassName}>", MiddleClassName.ToPlural(), isVirtual: true);
                        break;
                    case "O2M":
                        class2.AddProperty("string", class1.Name + "Id");
                        class2.AddProperty(class1.Name, class1.Name, isVirtual: true).AddAttributeToProperty("ForeignKey", $"\"{class1.Name + "Id"}\"").AddRequiredNameSpaces("System.ComponentModel.DataAnnotations.Schema");
                        class1.AddProperty($"IEnumerable<{class2.Name}>", class2.Name.ToPlural(), isVirtual: true);

                        break;
                    case "O2O":
                        class2.AddProperty("string", class1.Name + "Id");
                        class1.AddProperty(class2.Name, class2.Name, isVirtual: true);
                        class2.AddProperty(class1.Name, class1.Name, isVirtual: true);
                        break;
                    default:
                        break;
                }
            }
            relationShips.AddRange(Appendrelations);
        }

		/// <summary>
		/// Adding Relationships between MiddleClasses and Entity classes
		/// </summary>
		/// <param name="proj">The project in which the entity class is built.</param>
		/// <param name="relationShips">The relation section of config informations with MiddleClasses relations.</param>
		public static void AddMiddleClassesrelations(this Project proj, IEnumerable<RelationShip> relationShips)
        {
            var relations = new List<RelationShip>();
            var MiddleClasses = proj.CsFiles.GetMiddleClasses();
            foreach (var MiddleClass in MiddleClasses)
            {
                var MiddleClassesrelations = relationShips.GetrelationShipsByEntity(MiddleClass.Name);
                relations.AddRange(MiddleClassesrelations);
            }
            proj.Addrelations(ref relations);
        }
		/// <summary>
		/// Create An Entity section for MiddleClass
		/// </summary>
		/// <param name="entity1">the first entity class name.</param>
		/// <param name="entity2">the seccond entity class name</param>
		/// <returns>
		/// The Domains Object with the MiddleClass added
		/// </returns>
		public static Domains CreateMiddleClass(string entity1,string entity2)
        {
            
            var MiddleClassName = entity1 + entity2;
            var MiddleClass = new Entity()
            {
                EntityName = MiddleClassName,
                Cols = new List<Col>()
            };
            var Domain = new Domains()
            {
                Entites = new List<Entity>(),
                RelationShips = new List<RelationShip>()
            };
            var relationWithEntity1 = new RelationShip()
            {
                Entity1 = entity1,
                Entity2 = MiddleClassName,
                RelationShipType = "O2M"
            };
            var relationWithEntity2 = new RelationShip()
            {
                Entity1 = entity2,
                Entity2 = MiddleClassName,
                RelationShipType = "O2M"
            };
            Domain.Entites.Add(MiddleClass);
            Domain.RelationShips.Add(relationWithEntity1);
            Domain.RelationShips.Add(relationWithEntity2);
            return Domain;
        }

		/// <summary>
		/// Get the MiddleClasses of all class in the project
		/// </summary>
		/// <param name="classes">Get all Csharp files of a project</param>
		/// <returns>
		/// The IEnumerable of MiddleClasses
		/// </returns>
		public static IEnumerable<Class> GetMiddleClasses(this IEnumerable<CsFile> classes)
        {
            var _classes = classes.OfType<Class>().Where(p => p.Options.Any(t => t == "MiddleClass")).ToList();

            return _classes;
        }
    }

}