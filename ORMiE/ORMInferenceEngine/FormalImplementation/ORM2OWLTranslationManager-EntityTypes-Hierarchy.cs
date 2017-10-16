﻿using System;
using Microsoft.VisualStudio.Modeling;
using ORMSolutions.ORMArchitect.Core.ObjectModel;
using org.semanticweb.owlapi.model;
using org.semanticweb.owlapi.reasoner;
using uk.ac.manchester.cs.jfact;
using System.Collections.Generic;
using org.unibz.ucmf.askAPI;
using System.Collections;

namespace unibz.ORMInferenceEngine
{
	partial class ORM2OWLTranslationManager
	{


		
		void CreateInferredEntityTypesHierarchy(InferredConstraints container, InferredHierarchyTypes hierarchyTypes)
		{
			ORMModel model = container.Model;
			Store store = model.Store;
			Partition hierarchyPartition = Partition.FindByAlternateId(store, typeof(Hierarchy));

			if (hierarchyPartition == null)
			{
				hierarchyPartition = new Partition(store);
				hierarchyPartition.AlternateId = typeof(Hierarchy);
			}

			InferredHierarchy hierarchyContainer = InferredHierarchyIsForORMModel.GetInferredHierarchy(model);
			if (hierarchyContainer == null)
			{
				hierarchyContainer = new InferredHierarchy(hierarchyPartition);
				hierarchyContainer.Model = model;
			}
			else
			{
				hierarchyContainer.TopObjectTypeCollection.Clear();
			}

			java.util.ArrayList fathers = hierarchyTypes.getFathers();

			//For each father
			foreach (String father in fathers)
			{

				//Get the ObjectType of the father
				ObjectType objectType = findObjectTypeByName(model, father);
				//Get him to the top level
				TopLevelObjectType topLevelObjectType = new TopLevelObjectType(hierarchyContainer, objectType);
				if (hierarchyTypes.getSons(father).size() > 0)
				{
					foreach (String son in hierarchyTypes.getSons(father))
					{
						ObjectType subObjectType = findObjectTypeByName(model, son);
						new ObjectTypeContainment(topLevelObjectType, subObjectType);
					}
				}
			}
		}
	}
}