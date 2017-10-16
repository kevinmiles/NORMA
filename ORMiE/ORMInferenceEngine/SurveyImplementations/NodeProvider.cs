﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using ORMSolutions.ORMArchitect.Core.ObjectModel;
using ORMSolutions.ORMArchitect.Framework;
using ORMSolutions.ORMArchitect.Framework.Shell.DynamicSurveyTreeGrid;
using Microsoft.VisualStudio.Modeling;

namespace unibz.ORMInferenceEngine
{
	partial class ORMInferenceEngineDomainModel : ISurveyNodeProvider
	{
		#region ISurveyNodeProvider Implementation
		IEnumerable<object> ISurveyNodeProvider.GetSurveyNodes(object context, object expansionKey)
		{
			if (expansionKey == null)
			{
				foreach (TopLevelObjectType objectTypeLink in Store.ElementDirectory.FindElements<TopLevelObjectType>(false))
				{
					yield return objectTypeLink;
				}
				foreach (UnsatisfiableObjectType objectTypeLink in Store.ElementDirectory.FindElements<UnsatisfiableObjectType>(false))
				{
					yield return objectTypeLink;
				}
				foreach (UnsatisfiableFactType factTypeLink in Store.ElementDirectory.FindElements<UnsatisfiableFactType>(false))
				{
					yield return factTypeLink;
				}
				foreach (InferredConstraints container in Store.ElementDirectory.FindElements<InferredConstraints>(false))
				{
					foreach (SetComparisonConstraint constraint in container.SetComparisonConstraintCollection)
					{
						yield return constraint;
					}
					foreach (SetConstraint constraint in container.SetConstraintCollection)
					{
						yield return constraint;
					}
				}
			}
			else if (expansionKey == TopLevelObjectType.SurveyExpansionKey)
			{
				TopLevelObjectType objectTypeLink = (TopLevelObjectType)context;
				foreach (ObjectTypeContainment childLink in ObjectTypeContainment.GetLinksToChildCollection(objectTypeLink))
				{
					yield return childLink;
				}
			}
		}
		bool ISurveyNodeProvider.IsSurveyNodeExpandable(object context, object expansionKey)
		{
			if (expansionKey == TopLevelObjectType.SurveyExpansionKey)
			{
				TopLevelObjectType link = (TopLevelObjectType)context;
				return ObjectTypeContainment.GetChildCollection(link).Count != 0;
			}
			return false;
		}
		#endregion // ISurveyNodeProvider Implementation
		#region Survey Event Handlers
		private void ManageSurveyQuestionEventHandlers(ModelingEventManager eventManager, EventHandlerAction action)
		{
			Store store = Store;
			DomainDataDirectory directory = store.DomainDataDirectory;

			// Add survey events
			DomainClassInfo classInfo = directory.FindDomainRelationship(SetConstraintIsInferred.DomainClassId);
			eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(InferredSetConstraintAddedEvent), action);
			classInfo = directory.FindDomainRelationship(SetComparisonConstraintIsInferred.DomainClassId);
			eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(InferredSetComparisonConstraintAddedEvent), action);
			classInfo = directory.FindDomainRelationship(SubtypeFactIsInferred.DomainClassId);
			eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(InferredSubtypeFactAddedEvent), action);

			classInfo = directory.FindDomainRelationship(UnsatisfiableObjectType.DomainClassId);
			eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(UnsatisfiableObjectTypeAddedEvent), action);
			classInfo = directory.FindDomainRelationship(UnsatisfiableFactType.DomainClassId);
			eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(UnsatisfiableFactTypeAddedEvent), action);
		}
		/// <summary>
		/// Standard handler when an element needs to be removed from the model browser
		/// </summary>
		private static void ModelElementRemovedEvent(object sender, ElementDeletedEventArgs e)
		{
			INotifySurveyElementChanged eventNotify;
			ModelElement element = e.ModelElement;
			if (null != element && null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
			{
				eventNotify.ElementDeleted(element);
			}
		}
		/// <summary>
		/// Survey event handler for addition of a <see cref="UnsatisfiableObjectType"/>
		/// </summary>
		private static void UnsatisfiableObjectTypeAddedEvent(object sender, ElementAddedEventArgs e)
		{
			INotifySurveyElementChanged eventNotify;
			ModelElement element = e.ModelElement;
			if (null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
			{
				ObjectType objectType = ((UnsatisfiableObjectType)element).ObjectType;
				eventNotify.ElementAdded(objectType, null);
			}
		}
		/// <summary>
		/// Survey event handler for addition of a <see cref="UnsatisfiableFactType"/>
		/// </summary>
		private static void UnsatisfiableFactTypeAddedEvent(object sender, ElementAddedEventArgs e)
		{
			INotifySurveyElementChanged eventNotify;
			ModelElement element = e.ModelElement;
			if (null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
			{
				FactType factType = ((UnsatisfiableFactType)element).FactType;
				eventNotify.ElementAdded(factType, null);
			}
		}		/// <summary>
		/// Survey event handler for addition of a <see cref="SetConstraintIsInferred"/>
		/// </summary>
		private static void InferredSetConstraintAddedEvent(object sender, ElementAddedEventArgs e)
		{
			INotifySurveyElementChanged eventNotify;
			ModelElement element = e.ModelElement;
			if (null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
			{
				SetConstraint constraint = ((SetConstraintIsInferred)element).SetConstraint;
				switch (((IConstraint)constraint).ConstraintType)
				{
					case ConstraintType.SimpleMandatory:
					case ConstraintType.InternalUniqueness:
						LinkedElementCollection<Role> roles = constraint.RoleCollection;
						Role role;
						if (roles.Count == 1 &&
							((role = roles[0]) is SubtypeMetaRole || role is SupertypeMetaRole))
						{
							// Do not add an implied constraint
							return;
						}
						break;
				}
				eventNotify.ElementAdded(constraint, null);
			}
		}
		/// <summary>
		/// Survey event handler for addition of a <see cref="SetComparisonConstraintIsInferred"/>
		/// </summary>
		private static void InferredSetComparisonConstraintAddedEvent(object sender, ElementAddedEventArgs e)
		{
			INotifySurveyElementChanged eventNotify;
			ModelElement element = e.ModelElement;
			if (null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
			{
				SetComparisonConstraint constraint = ((SetComparisonConstraintIsInferred)element).SetComparisonConstraint;
				ExclusionConstraint exclusion;
				//do not add the exclusion constraint if its part of ExclusiveOr. 
				if (null != (exclusion = constraint as ExclusionConstraint) && null != exclusion.ExclusiveOrMandatoryConstraint)
				{
					return;
				}
				eventNotify.ElementAdded(constraint, null);
			}
		}
		/// <summary>
		/// Survey event handler for addition of a <see cref="SubtypeFactIsInferred"/>
		/// </summary>
		private static void InferredSubtypeFactAddedEvent(object sender, ElementAddedEventArgs e)
		{
			INotifySurveyElementChanged eventNotify;
			ModelElement element = e.ModelElement;
			if (null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
			{
				SubtypeFact subtypefact = ((SubtypeFactIsInferred)element).SubtypeFact;
				eventNotify.ElementAdded(subtypefact, null);
			}
		}
		#endregion // Survey Event Handlers
		#region Color Provider
		private static void GetItemColor(SurveyRootElementType elementType, ref Color foreColor, ref Color backColor)
		{
			// UNDONE: This is a place holder sample for adding color to the model browser using survey categories.
			// To do this properly, the colors should be user-settable, which requires you to associated a color provider
			// with Visual Studio for your package. See the ORMDesignerFontsAndColors class in the core for an example.
			// Note that these will also affect your pkgdef file.
			switch (elementType)
			{
				case SurveyRootElementType.InferredConstraint:
					foreColor = Color.Green;
					break;
				case SurveyRootElementType.TopLevelObjectType:
					//UNDONE: What about conditional coloring?? 
					break;
				case SurveyRootElementType.UnsatisfiableElement:
					foreColor = Color.Red;
					break;
			}
		}
		#endregion // Color Provider
	}
}
