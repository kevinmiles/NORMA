﻿using System;
using System.Drawing;
using System.Windows.Forms;
using ORMSolutions.ORMArchitect.Core.ObjectModel;
using ORMSolutions.ORMArchitect.Core.ShapeModel;
using ORMSolutions.ORMArchitect.Framework.Diagrams;
using ORMSolutions.ORMArchitect.Framework;
using Microsoft.VisualStudio.Modeling;
using org.semanticweb.owlapi.model;
using org.semanticweb.owlapi.reasoner;
using org.semanticweb.owlapi.reasoner.impl;
using System.ComponentModel;
using ORMSolutions.ORMArchitect.Framework.Shell.DynamicSurveyTreeGrid;

namespace unibz.ORMInferenceEngine
{
	partial class Hierarchy
	{
        ORM2OWLTranslationManager translationManager = new ORM2OWLTranslationManager();
		//ORMInferenceGenerator inferenceGenerator = new ORMInferenceGenerator();

		#region Deserialization Fixup Classes
		/// <summary>
		/// A <see cref="IDeserializationFixupListener"/> for synchronizing the abstraction model on load
		/// </summary>
		public static IDeserializationFixupListener FixupListener
		{
			get
			{
				return new HierachyFixupListener();
			}
		}
		/// <summary>
		/// Fixup listener implementation.
		/// </summary>
		private sealed class HierachyFixupListener : DeserializationFixupListener<ORMModel>
		{
			/// <summary>
			/// ORMModelFixupListener constructor
			/// </summary>
			public HierachyFixupListener()
				: base((int)ORMInferenceEngineDeserializationFixupPhase.CreateHierarchy)
			{
			}
			/// <summary>
			/// Make sure we have our tracker attached to all loaded models.
			/// </summary>
			/// <param name="element">An ORMModel element</param>
			/// <param name="store">The context store</param>
			/// <param name="notifyAdded">The listener to notify if elements are added during fixup</param>
			protected sealed override void ProcessElement(ORMModel element, Store store, INotifyElementAdded notifyAdded)
			{
				Hierarchy hierarchy = new Hierarchy(store);
				hierarchy.Model = element;

                hierarchy.rebuildHierarchyAndUnsatDomain(element);
				notifyAdded.ElementAdded(hierarchy, true);
                
			}
		}
		#endregion // Deserialization Fixup Classes
        
		#region Rule Methods
        // We are using new the mechanism of events, not rules
        ///// <summary>
        ///// AddRule: typeof(ORMSolutions.ORMArchitect.Core.ObjectModel.ModelHasObjectType)
        ///// <!--Dropping a new entity type -->
        ///// </summary>
        //private static void ObjectTypeAddedRule(ElementAddedEventArgs e)
        //{
        //    FrameworkDomainModel.DelayValidateElement((ModelHasObjectType)e.ModelElement, DelayValidateObjectTypeAdded);
        //}
        //private static void DelayValidateObjectTypeAdded(ModelElement element)
        //{
        //    if (!element.IsDeleted)
        //    {
        //        ObjectType objectType = ((ModelHasObjectType)element).ObjectType;
        //        ORMModel model = objectType.Model;

        //        Store store = element.Store;
        //        Partition hierarchyPartition = Partition.FindByAlternateId(store, typeof(Hierarchy));
        //        if (hierarchyPartition == null)
        //        {
        //            hierarchyPartition = new Partition(store);
        //            hierarchyPartition.AlternateId = typeof(Hierarchy);
        //        }

        //        Objectification objectification;
        //        if (!(objectType.IsImplicitBooleanValue ||
        //             (null != (objectification = objectType.Objectification) &&
        //              objectification.IsImplied)))
        //        {
        //            InferredHierarchy hierarchyContainer = InferredHierarchyIsForORMModel.GetInferredHierarchy(model);
        //            if (hierarchyContainer == null)
        //            {
        //                hierarchyContainer = new InferredHierarchy(hierarchyPartition);
        //                hierarchyContainer.Model = model;
        //            }
        //            else
        //            {
        //                hierarchyContainer.TopObjectTypeCollection.Clear();
        //            }

        //            Hierarchy hierarchy = HierarchyIsForORMModel.GetHierarchy(model);
        //            OWLOntology ontology = hierarchy.translationManager.translateToOWL(model);
        //            OWLOntologyManager manager = ontology.getOWLOntologyManager();
        //            OWLReasoner reasoner = hierarchy.inferenceGenerator.getPrecomputedReasoner(ontology);

        //            OWLClassNode unsatClasses = reasoner.getUnsatisfiableClasses() as OWLClassNode;
        //            OWLClassExpression thing = manager.getOWLDataFactory().getOWLThing();
        //            java.util.Set classes = reasoner.getSubClasses(thing, false).getFlattened();
        //            foreach (OWLClass clazz in classes.toArray())
        //            {
        //                if (unsatClasses.contains(clazz))
        //                    continue;

        //                objectType = ORMInferenceGenerator.findObjectTypeByName(model, clazz.getIRI().getFragment());
        //                if (null == objectType || objectType.IsValueType)
        //                    continue;

        //                TopLevelObjectType topLevelObjectType = new TopLevelObjectType(hierarchyContainer, objectType);
        //                java.util.Set subclasses = reasoner.getSubClasses(clazz, true).getFlattened();

        //                foreach (OWLClass subclazz in subclasses.toArray())
        //                {
        //                    if (unsatClasses.contains(subclazz))
        //                        continue;

        //                    ObjectType subObjectType = ORMInferenceGenerator.findObjectTypeByName(model, subclazz.getIRI().getFragment());
        //                    if (null == subObjectType)
        //                        continue;

        //                    new ObjectTypeContainment(topLevelObjectType, subObjectType);
        //                }
        //            }
        //        }
        //    }
        //}
		#endregion // Rule Methods

         #region Event Integration
         /// <summary>
         /// Integrate state change event handlers.
         /// </summary>
         public static void ManageModelStateEventHandlers(Store store, ModelingEventManager eventManager, EventHandlerAction action)
         {
             DomainDataDirectory directory = store.DomainDataDirectory;
             EventHandler<ElementDeletedEventArgs> standardDeleteHandler = new EventHandler<ElementDeletedEventArgs>(ModelElementRemovedEvent);
             EventHandler<ElementDeletedEventArgs> recalculateDeleteHandler = new EventHandler<ElementDeletedEventArgs>(ModelElementRemovedRecalculateEvent);

             DomainClassInfo classInfo;
             DomainPropertyInfo propertyInfo;

             //Inferred Hierarchy
             classInfo = directory.FindDomainClass(TopLevelObjectType.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(TopLevelObjectTypeAddedEvent), action);
             eventManager.AddOrRemoveHandler(classInfo, standardDeleteHandler, action);
             classInfo = directory.FindDomainClass(ObjectTypeContainment.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(ObjectTypeContainmentAddedEvent), action);
             eventManager.AddOrRemoveHandler(classInfo, standardDeleteHandler, action);

             //SubType
             propertyInfo = directory.FindDomainProperty(SubtypeFact.ProvidesPreferredIdentifierDomainPropertyId);
             eventManager.AddOrRemoveHandler(propertyInfo, new EventHandler<ElementPropertyChangedEventArgs>(SubtypeFactAddedEvent), action);
             classInfo = directory.FindDomainClass(SubtypeFact.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, recalculateDeleteHandler, action);

             ////Object Type
             classInfo = directory.FindDomainClass(ObjectType.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, recalculateDeleteHandler, action);
             classInfo = directory.FindDomainRelationship(ModelHasObjectType.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(ObjectTypeAddedEvent), action);

             //Fact Type
             classInfo = directory.FindDomainClass(FactType.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, recalculateDeleteHandler, action);
             //classInfo = directory.FindDomainRelationship(ModelHasFactType.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(FactTypeAddedEvent), action);

             //Uniqueness Constraint
             classInfo = directory.FindDomainRelationship(UniquenessConstraintHasNMinusOneError.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(UniquenessConstraintHasNMinusOneErrorDeletedEvent), action);
             
             //Set Constraint
             //classInfo = directory.FindDomainRelationship(ModelHasSetConstraint.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(SetConstraintAddedEvent), action);
             classInfo = directory.FindDomainClass(SetConstraint.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, recalculateDeleteHandler, action);
             classInfo = directory.FindDomainClass(ConstraintRoleSequenceHasRole.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(ConstraintRoleSequenceHasRoleAddedEvent), action);
             classInfo = directory.FindDomainRelationship(SetConstraintHasTooFewRoleSequencesError.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(SetCompletedEvent), action);
            
             //Set Comparison
             //classInfo = directory.FindDomainRelationship(ModelHasSetComparisonConstraint.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(SetComparisonConstraintAddedEvent), action);
             classInfo = directory.FindDomainClass(SetComparisonConstraint.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, recalculateDeleteHandler, action);
             classInfo = directory.FindDomainRelationship(SetComparisonConstraintHasTooFewRoleSequencesError.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(SetComparisonCompletedEvent), action);
             classInfo = directory.FindDomainRelationship(SetComparisonConstraintHasExternalConstraintRoleSequenceArityMismatchError.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(SetComparisonCompletedEvent), action);


             // External constraint expansion
             classInfo = directory.FindDomainRelationship(FactConstraint.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(FactConstraintAddedEvent), action);
             eventManager.AddOrRemoveHandler(classInfo, recalculateDeleteHandler, action);

             ////FactTypeHasRole
             //classInfo = directory.FindDomainClass(FactTypeHasRole.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(FactTypeHasRoleAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(FactTypeHasRoleDeletedEvent), action);

             ////Role player changes
             propertyInfo = directory.FindDomainProperty(Role.IsMandatoryDomainPropertyId);
             eventManager.AddOrRemoveHandler(propertyInfo, new EventHandler<ElementPropertyChangedEventArgs>(IsMandatoryChangedEvent), action);
             //classInfo = directory.FindDomainClass(ObjectTypePlaysRole.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(RolePlayerAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(RolePlayerDeletedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<RolePlayerChangedEventArgs>(RolePlayerRolePlayerChangedEvent), action);

             //Error state changed
             classInfo = directory.FindDomainRelationship(RoleHasRolePlayerRequiredError.DomainClassId);
             eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(RoleHasRolePlayerRequiredErrorDeletedEvent), action);
             //classInfo = directory.FindDomainRelationship(ElementAssociatedWithModelError.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(ModelElementErrorStateChangedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(FactTypeHasFactTypeInstance.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(ObjectTypeHasObjectTypeInstance.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(ValueConstraintHasValueRange.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(RolePathOwnerOwnsLeadRolePath.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathRolePlayedChangedHandler, action);
             //classInfo = directory.FindDomainRelationship(RoleSubPathIsContinuationOfRolePath.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathRolePlayedChangedHandler, action);
             //classInfo = directory.FindDomainRelationship(PathedRole.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(PathedRoleHasValueConstraint.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(LeadRolePathCalculatesCalculatedPathValue.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(FactTypeHasDerivationRule.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(SubtypeHasDerivationRule.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(ConstraintRoleSequenceHasJoinPath.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //classInfo = directory.FindDomainRelationship(SetComparisonConstraintHasRoleSequence.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);

             ////ModalityChanged
             //propertyInfo = directory.FindDomainProperty(SetConstraint.ModalityDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardGlyphChangeHandler, action);
             //propertyInfo = directory.FindDomainProperty(SetComparisonConstraint.ModalityDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardGlyphChangeHandler, action);
             //propertyInfo = directory.FindDomainProperty(CardinalityConstraint.ModalityDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardGlyphChangeHandler, action);

             ////RingType changed
             //propertyInfo = directory.FindDomainProperty(RingConstraint.RingTypeDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardGlyphChangeHandler, action);

             ////ValueComparison Operator changed
             //propertyInfo = directory.FindDomainProperty(ValueComparisonConstraint.OperatorDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardGlyphChangeHandler, action);

             ////Preferred Identifier Changed
             //propertyInfo = directory.FindDomainProperty(UniquenessConstraint.IsPreferredDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardGlyphChangeHandler, action);

             //classInfo = directory.FindDomainClass(EntityTypeHasPreferredIdentifier.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, standardErrorPathDeletedHandler, action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<RolePlayerChangedEventArgs>(PreferredIdentifierRolePlayerChangedEvent), action);

             ////ExclusiveOr added deleted 
             //classInfo = directory.FindDomainClass(ExclusiveOrConstraintCoupler.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(ExclusiveOrAddedEvent), action);
             //classInfo = directory.FindDomainClass(ExclusiveOrConstraintCoupler.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(ExclusiveOrDeletedEvent), action);
             //propertyInfo = directory.FindDomainProperty(ExclusionConstraint.NameDomainPropertyId);
             //classInfo = directory.FindDomainClass(ExclusionConstraint.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, propertyInfo, new EventHandler<ElementPropertyChangedEventArgs>(ExclusiveOrNameChangedEvent), action);

             ////Grouping
             //classInfo = directory.FindDomainClass(ElementGrouping.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(GroupingAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, standardDeleteHandler, action);
             //propertyInfo = directory.FindDomainProperty(ElementGrouping.NameDomainPropertyId);
             //eventManager.AddOrRemoveHandler(propertyInfo, standardNameChangedHandler, action);
             //classInfo = directory.FindDomainRelationship(GroupingElementRelationship.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(GroupingElementAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(GroupingElementDeletedEvent), action);
             //classInfo = directory.FindDomainRelationship(ElementGroupingContainsElementGrouping.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(NestedGroupingAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(NestedGroupingDeletedEvent), action);
             //classInfo = directory.FindDomainRelationship(ElementGroupingIsOfElementGroupingType.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(GroupingTypeAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(GroupingTypeDeletedEvent), action);
             //classInfo = directory.FindDomainRelationship(GroupingMembershipContradictionErrorIsForElement.DomainClassId);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementAddedEventArgs>(GroupingContradictionErrorAddedEvent), action);
             //eventManager.AddOrRemoveHandler(classInfo, new EventHandler<ElementDeletedEventArgs>(GroupingContradictionErrorDeletedEvent), action);
         }

         /// <summary>
         /// Survey event handler for addition of an <see cref="TopLevelObjectType"/>
         /// </summary>
         private static void TopLevelObjectTypeAddedEvent(object sender, ElementAddedEventArgs e)
         {
             INotifySurveyElementChanged eventNotify;
             TopLevelObjectType topObjType = (TopLevelObjectType)e.ModelElement;
             if (!topObjType.IsDeleted && null != (eventNotify = (e.ModelElement.Store as IORMToolServices).NotifySurveyElementChanged))
             {
                 eventNotify.ElementAdded(topObjType, null);
             }
         }
         
         /// <summary>
         /// Survey event handler for addition of an <see cref="ObjectTypeContainment"/>
         /// </summary>
         private static void ObjectTypeContainmentAddedEvent(object sender, ElementAddedEventArgs e)
         {
             INotifySurveyElementChanged eventNotify;
             ObjectTypeContainment objTypeCont = (ObjectTypeContainment)e.ModelElement;
             if (!objTypeCont.IsDeleted && null != (eventNotify = (e.ModelElement.Store as IORMToolServices).NotifySurveyElementChanged))
             {
                 eventNotify.ElementAdded(objTypeCont, objTypeCont.Parent);
             }
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
         /// Handler which requires recalculation when an element needs to be removed from the model browser
         /// </summary>
         private static void ModelElementRemovedRecalculateEvent(object sender, ElementDeletedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (null != element)
             {
                 Store store = (Store)sender;
                 WorkerThreadAddRecalculation(store);
             }
         }

         /// <summary>
         /// Survey event handler for addition of an <see cref="ModelHasObjectType"/>
         /// </summary>
         private static void ObjectTypeAddedEvent(object sender, ElementAddedEventArgs e)
         {
             INotifySurveyElementChanged eventNotify;
             ModelElement element = e.ModelElement;
             if (null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
             {
                 ObjectType objectType = ((ModelHasObjectType)element).ObjectType;
                 Objectification objectification;
                 if (!objectType.IsImplicitBooleanValue && (null == (objectification = objectType.Objectification) || !objectification.IsImplied))
                 {
                     WorkerThreadAddRecalculation(objectType.Model);
                 }
             }
         }

         /// <summary>
         /// Survey event handler for addition of an <see cref="SubtypeFact.ProvidesPreferredIdentifierDomainPropertyId"/>
         /// </summary>
         private static void SubtypeFactAddedEvent(object sender, ElementPropertyChangedEventArgs e)
         {
             INotifySurveyElementChanged eventNotify;
             ModelElement element = e.ModelElement;
             if (!element.IsDeleted && 
                 null != (eventNotify = (element.Store as IORMToolServices).NotifySurveyElementChanged))
             {
                if (element.GetType() == typeof(SubtypeFact))
                {
                    // Add processing for the worker thread.
                    WorkerThreadAddRecalculation(((SubtypeFact)element).Model);
                }
             }
         }

         /// <summary>
         /// Survey event handler for deletion of an <see cref="RoleHasRolePlayerRequiredError"/>
         /// </summary>
         private static void RoleHasRolePlayerRequiredErrorDeletedEvent(object sender, ElementDeletedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (null != element)
             {
                 if (element.GetType() == typeof(RoleHasRolePlayerRequiredError))
                 {
                     RoleHasRolePlayerRequiredError error = (RoleHasRolePlayerRequiredError)element;
                     Role role = error.Role;
                     FactType factType = (role != null) ? role.FactType : null;
                     if (factType != null)
                     {
                         bool recalc = true;
                         foreach (Role rl in factType.RoleCollection)
                         {
                             if (!rl.Equals(role) && rl.RolePlayerRequiredError != null)
                                 recalc = false;
                         }
                         if(recalc)
                            WorkerThreadAddRecalculation((Store)sender);
                     }
                 }
             }
         }

         /// <summary>
         /// Survey event handler for deletion of an <see cref="UniquenessConstraintHasNMinusOneError"/>
         /// </summary>
         private static void UniquenessConstraintHasNMinusOneErrorDeletedEvent(object sender, ElementDeletedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (null != element)
             {
                 if (element.GetType() == typeof(UniquenessConstraintHasNMinusOneError))
                 {
                     UniquenessConstraintHasNMinusOneError error = (UniquenessConstraintHasNMinusOneError)element;
                     UniquenessConstraint uqc = error.Constraint;
                     if (uqc.TooFewRoleSequencesError == null && uqc.TooManyRoleSequencesError == null)
                     {
                         WorkerThreadAddRecalculation((Store)sender);
                     }
                 }
             }
         }

         /// <summary>
         /// Survey event handler for addition of an <see cref="Role.IsMandatoryDomainPropertyId"/>
         /// </summary>
         private static void IsMandatoryChangedEvent(object sender, ElementPropertyChangedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (!element.IsDeleted)
             {
                 if (element.GetType() == typeof(Role))
                 {
                     // Add processing for the worker thread.
                     WorkerThreadAddRecalculation(((Role)element).Store);
                 }
             }
         }

         /// <summary>
         /// Survey event handler for addition of an <see cref="ConstraintRoleSequenceHasRole"/>
         /// </summary>
         private static void ConstraintRoleSequenceHasRoleAddedEvent(object sender, ElementAddedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (!element.IsDeleted)
             {
                 ConstraintRoleSequence crs = ((ConstraintRoleSequenceHasRole)element).ConstraintRoleSequence;
                 if (crs.Constraint != null && crs.Constraint.GetType() == typeof(UniquenessConstraint))
                 {
                     UniquenessConstraint uq = (UniquenessConstraint)crs.Constraint;
                     if (uq.TooFewRoleSequencesError == null && uq.TooManyRoleSequencesError == null && uq.NMinusOneError == null)
                     {
                         WorkerThreadAddRecalculation(uq.Model);
                     }
                 }
             }
         }

         /// <summary>
         /// Survey event handler for completing of an <see cref="SetComparisonConstraint"/>
         /// </summary>
         private static void SetComparisonCompletedEvent(object sender, ElementDeletedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (null != element)
             {
                 SetComparisonConstraint scc = null;
                 if (element.GetType() == typeof(SetComparisonConstraintHasExternalConstraintRoleSequenceArityMismatchError))
                     scc = ((SetComparisonConstraintHasExternalConstraintRoleSequenceArityMismatchError)element).Constraint;
                 if (element.GetType() == typeof(SetComparisonConstraintHasTooFewRoleSequencesError))
                     scc = ((SetComparisonConstraintHasTooFewRoleSequencesError)element).SetComparisonConstraint;
                  
                 if (scc != null && scc.TooFewRoleSequencesError == null && scc.ArityMismatchError == null)
                 {
                     WorkerThreadAddRecalculation((Store)sender);
                 }
             }
         }

         /// <summary>
         /// Survey event handler for completing of an <see cref="SetConstraint"/>
         /// </summary>
         private static void SetCompletedEvent(object sender, ElementDeletedEventArgs e)
         {
             ModelElement element = e.ModelElement;
             if (null != element)
             {
                 SetConstraint sc = null;
                 if (element.GetType() == typeof(SetConstraintHasTooFewRoleSequencesError))
                     sc = ((SetConstraintHasTooFewRoleSequencesError)element).SetConstraint;
                 if (sc != null && sc.TooFewRoleSequencesError == null)
                 {
                     WorkerThreadAddRecalculation((Store)sender);
                 }
             }
         }

         BackgroundWorker myWorker;
         public BackgroundWorker getWorker()
         { return myWorker; }
         public void setWorker(BackgroundWorker worker)
         { myWorker = worker; }


         public static void startInferringHierarchy(Hierarchy hierarchy)
         {
             if (!hierarchy.IsDeleted)
             {
                 Store store = hierarchy.Store;
                 RuleManager ruleMgr = store.RuleManager;
                 Type ruleType = typeof(Microsoft.VisualStudio.Modeling.Diagrams.Diagram);
                 ruleType = ruleType.Assembly.GetType(ruleType.Namespace + Type.Delimiter + "DiagramCommittingRule");
                 try
                 {
                     ruleMgr.DisableRule(ruleType);
                     using (Transaction t = store.TransactionManager.BeginTransaction(Resources.Hierarchy_StartHierarchy_TransactionName))
                     {
                         Partition partition = hierarchy.Partition;
                         ORMModel model = hierarchy.Model;
                         hierarchy.rebuildHierarchyAndUnsatDomain(model);
                         t.Commit();
                     }
                 }
                 finally
                 {
                     ruleMgr.EnableRule(ruleType);
                 }
             }
         }

         private static void ProcessingCompleted(object sender, RunWorkerCompletedEventArgs e)
         {
             //return;

             Store store;
             Hierarchy contextHierarchy; // UNDONE: Temporary placeholder for data object, return real data or attach it to ProcessInferenceResult, which is in the sender object
             if (!e.Cancelled &&
                 null != (contextHierarchy = e.Result as Hierarchy) &&
                 null != (store = Utility.ValidateStore(contextHierarchy.Store)) &&
                 !contextHierarchy.IsDeleted)
             {
                 contextHierarchy.myWorker = null;
                 Action<Hierarchy> doTransaction = startInferringHierarchy;
                 TransactionManager txMgr = store.TransactionManager;
                 // See if we're in a transaction currently and add a temporary TransactionCompleted
                 // event handler to process this later.
                 if (txMgr.InTransaction)
                 {
                     if (txMgr.CurrentTransaction.Name != "Start Hierarchy")
                     {
                         EventHandler<TransactionEventArgs> transactionComplete = delegate(object completedSender, TransactionEventArgs txE)
                         {
                             doTransaction(contextHierarchy);
                         };
                         try
                         {
                             // UNDONE: Not sure if we should run this in transactionCompleted, or in ElementEventsEnded after the
                             // transaction is done.
                             store.EventManagerDirectory.TransactionCommitted.Add(transactionComplete);
                             store.EventManagerDirectory.TransactionRolledBack.Add(transactionComplete);
                         }
                         finally
                         {
                             store.EventManagerDirectory.TransactionCommitted.Remove(transactionComplete);
                             store.EventManagerDirectory.TransactionRolledBack.Remove(transactionComplete);
                         }
                     }
                 }
                 else
                 {
                     if (store.UndoManager.UndoableTransactions.Count > 0 && store.UndoManager.UndoableTransactions[0].Name != "Start Hierarchy")
                        doTransaction(contextHierarchy);
                 }
             }
         }
         #endregion // Event Integration

         #region Worker Thread
         private static void WorkerThreadAddRecalculation(ORMModel model)
         {
            Hierarchy hierarchy = HierarchyIsForORMModel.GetHierarchy(model);
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(HierarchyProcessor);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ProcessingCompleted);
            hierarchy.myWorker = worker;
            worker.RunWorkerAsync(hierarchy);
         }
         private static void WorkerThreadAddRecalculation(Store store)
         {
             Hierarchy hierarchy = store.ElementDirectory.FindElements<Hierarchy>()[0];
             BackgroundWorker worker = new BackgroundWorker();
             worker.DoWork += new DoWorkEventHandler(HierarchyProcessor);
             worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ProcessingCompleted);
             hierarchy.myWorker = worker;
             worker.RunWorkerAsync(hierarchy);
         }
         private static void HierarchyProcessor(object sender, DoWorkEventArgs e)
         {
             Hierarchy hierarchy = (Hierarchy)e.Argument;
             e.Result = hierarchy;
         }
         #endregion // Worker Thread

		//IMPORTANTE!!!
        void rebuildHierarchyAndUnsatDomain(ORMModel model)
		{
            Store store = model.Store;
            Partition hierarchyPartition = Partition.FindByAlternateId(store, typeof(Hierarchy));
            Partition unsatPartition = Partition.FindByAlternateId(store, typeof(UnsatisfiableDomain));

            if (hierarchyPartition == null)
            {
                hierarchyPartition = new Partition(store);
                hierarchyPartition.AlternateId = typeof(Hierarchy);
            }
            if (unsatPartition == null)
            {
                unsatPartition = new Partition(store);
                unsatPartition.AlternateId = typeof(UnsatisfiableDomain);
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

            InferredUnsatisfiableDomain unsatContainer = InferredUnsatisfiableDomainIsForORMModel.GetInferredUnsatisfiableDomain(model);
            if (unsatContainer == null)
            {
                unsatContainer = new InferredUnsatisfiableDomain(unsatPartition);
                unsatContainer.Model = model;
            }
            else
            {
                unsatContainer.ObjectTypeCollection.Clear();
                unsatContainer.FactTypeCollection.Clear();
            }


            //FactType factType = null;
            //ObjectType objectType = null;


			//Chiamato qua per la prima volta all'avvio
			//chiamato anche ogni volta che si riaggiorna automaticamente ad ogni inferenza
			//non cancellare il codice di Dmitri, perche' devi vedere come ha stampato a video la gerarchia e l'empty set
            translationManager.translateToOWL(model);

			/*
            OWLOntologyManager manager = ontology.getOWLOntologyManager();
            OWLReasoner reasoner = inferenceGenerator.getPrecomputedReasoner(ontology);

            OWLClassNode unsatClasses = reasoner.getUnsatisfiableClasses() as OWLClassNode;
            OWLClassExpression thing = manager.getOWLDataFactory().getOWLThing();
            java.util.Set classes = reasoner.getSubClasses(thing, false).getFlattened();

            foreach (OWLClass clazz in classes.toArray())
			{
                if (unsatClasses.contains(clazz))
                {
                    String name = clazz.getIRI().getFragment();
                    if (name.StartsWith("obj_"))
                    {
                        factType = ORMInferenceGenerator.findFactTypeByName(model, name.Substring(4));
                        if (null == factType)
                            continue;
                        new UnsatisfiableFactType(unsatContainer, factType);
                    }
                    else
                    {
                        objectType = ORMInferenceGenerator.findObjectTypeByName(model, name);
                        if (null == objectType)
                            continue;
                        new UnsatisfiableObjectType(unsatContainer, objectType);
                    }
                    continue;
                }

				objectType = ORMInferenceGenerator.findObjectTypeByName(model, clazz.getIRI().getFragment());
				if (null == objectType || objectType.IsValueType)
					continue;

				TopLevelObjectType topLevelObjectType = new TopLevelObjectType(hierarchyContainer, objectType);
				java.util.Set subclasses = reasoner.getSubClasses(clazz, true).getFlattened();

				foreach (OWLClass subclazz in subclasses.toArray())
				{
					if (unsatClasses.contains(subclazz))
						continue;

					ObjectType subObjectType = ORMInferenceGenerator.findObjectTypeByName(model, subclazz.getIRI().getFragment());
					if (null == subObjectType)
						continue;

					new ObjectTypeContainment(topLevelObjectType, subObjectType);
				}
				
			}

	*/
		}

	};
}