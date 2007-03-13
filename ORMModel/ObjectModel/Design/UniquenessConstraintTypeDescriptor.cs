#region Common Public License Copyright Notice
/**************************************************************************\
* Neumont Object-Role Modeling Architect for Visual Studio                 *
*                                                                          *
* Copyright � Neumont University. All rights reserved.                     *
*                                                                          *
* The use and distribution terms for this software are covered by the      *
* Common Public License 1.0 (http://opensource.org/licenses/cpl) which     *
* can be found in the file CPL.txt at the root of this distribution.       *
* By using this software in any fashion, you are agreeing to be bound by   *
* the terms of this license.                                               *
*                                                                          *
* You must not remove this notice, or any other, from this software.       *
\**************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Permissions;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Design;
using Neumont.Tools.Modeling.Design;
using Neumont.Tools.ORM.ObjectModel;

namespace Neumont.Tools.ORM.ObjectModel.Design
{
	/// <summary>
	/// <see cref="ElementTypeDescriptor"/> for <see cref="UniquenessConstraint"/>s.
	/// </summary>
	[HostProtection(SecurityAction.LinkDemand, SharedState = true)]
	public class UniquenessConstraintTypeDescriptor<TModelElement> : ORMModelElementTypeDescriptor<TModelElement>
		where TModelElement : UniquenessConstraint
	{
		/// <summary>
		/// Initializes a new instance of <see cref="UniquenessConstraintTypeDescriptor{TModelElement}"/>
		/// for <paramref name="selectedElement"/>.
		/// </summary>
		public UniquenessConstraintTypeDescriptor(ICustomTypeDescriptor parent, TModelElement selectedElement)
			: base(parent, selectedElement)
		{
		}

		/// <summary>
		/// Display different class names for internal and external <see cref="UniquenessConstraint"/>s.
		/// </summary>
		public override string GetClassName()
		{
			return ModelElement.IsInternal ? ResourceStrings.InternalUniquenessConstraint : ResourceStrings.ExternalUniquenessConstraint;
		}
		/// <summary>
		/// Ensure that the <see cref="UniquenessConstraint.IsPreferred"/> property is read-only
		/// when the <see cref="FactType.InternalUniquenessConstraintChangeRule"/> is
		/// unable to make it <see langword="true"/>.
		/// Make sure the <see cref="SetConstraint.Modality">Modality</see> property
		/// is read only for single-role uniqueness constraints on the Objectification end of an implied fact type.
		/// </summary>
		protected override bool IsPropertyDescriptorReadOnly(ElementPropertyDescriptor propertyDescriptor)
		{
			UniquenessConstraint uniquenessConstraint;
			Guid propertyId = propertyDescriptor.DomainPropertyInfo.Id;
			LinkedElementCollection<Role> roles;
			FactType factType;
			if (propertyId == UniquenessConstraint.IsPreferredDomainPropertyId)
			{
				uniquenessConstraint = ModelElement;
				ObjectType identifierFor = uniquenessConstraint.PreferredIdentifierFor;
				if (identifierFor != null)
				{
					// If this is the preferred identifier for an objectifying type and
					// is the only alethic internal uniqueness constraint for the fact, then
					// it will automatically be readded as the preferred identifier if it is
					// changed to false. Don't allow it to change.
					if (uniquenessConstraint.IsInternal &&
						(null != (factType = identifierFor.NestedFactType)) &&
						// Note there is only fact with an internal constraint
						(factType == uniquenessConstraint.FactTypeCollection[0]))
					{
						UniquenessConstraint candidate = null;
						foreach (UniquenessConstraint constraint in factType.GetInternalConstraints<UniquenessConstraint>())
						{
							if (constraint.Modality == ConstraintModality.Alethic)
							{
								if (candidate != null || constraint != uniquenessConstraint)
								{
									candidate = null;
									break;
								}
								else
								{
									candidate = constraint;
								}
							}
						}
						return candidate != null;
					}
					return false;
				}
				else
				{
					return !uniquenessConstraint.TestAllowPreferred(null, false);
				}
			}
			else if (propertyId == UniquenessConstraint.ModalityDomainPropertyId &&
				(uniquenessConstraint = ModelElement).Store != null &&
				uniquenessConstraint.IsInternal &&
				(roles = uniquenessConstraint.RoleCollection).Count == 1 &&
				((factType = roles[0].FactType).ImpliedByObjectification != null ||
				factType is SubtypeFact))
			{
				return true;
			}
			return base.IsPropertyDescriptorReadOnly(propertyDescriptor);
		}
	}
}
