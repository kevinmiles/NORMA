using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using Northface.Tools.ORM.ObjectModel;
using Northface.Tools.ORM.Shell;
namespace Northface.Tools.ORM.ShapeModel
{
	#region (Temporary) CompositeLinkDecorator test class
	/// <summary>
	/// UNDONE: This is just a test to see if we can draw a filled circle contained inside
	/// a nested circle
	/// </summary>
	public class CircleInCircleLinkDecorator : CompositeLinkDecorator
	{
		/// <summary>
		/// Singleton instance of this decorator
		/// </summary>
		public static readonly LinkDecorator Decorator = new CircleInCircleLinkDecorator();
		private CircleInCircleLinkDecorator()
		{
		}
		/// <summary>
		/// Provides multiple paths for the composite decorator that are drawn in
		/// to produce the decorator.
		/// </summary>
		/// <value></value>
		protected override System.Collections.Generic.IList<LinkDecorator> DecoratorCollection
		{
			get
			{
				return new LinkDecorator[] { new InnerCircle(), new OuterCircle() };
			}
		}
		private class OuterCircle : LinkDecorator
		{
			public OuterCircle()
			{
				FillDecorator = true;
			}
			protected override GraphicsPath GetPath(RectangleD bounds)
			{
				GraphicsPath path = new GraphicsPath();
				path.AddArc(RectangleD.ToRectangleF(bounds), 0, 360);
				return path;
			}
			public override StyleSetResourceId BrushId
			{
				get
				{
					return DiagramBrushes.DiagramBackground;
				}
			}
		}
		private class InnerCircle : LinkDecorator
		{
			public InnerCircle()
			{
				FillDecorator = true;
			}
			protected override GraphicsPath GetPath(RectangleD bounds)
			{
				GraphicsPath path = new GraphicsPath();
				float inflateBy = -(float)(bounds.Width / 4);
				RectangleF boundsF = RectangleD.ToRectangleF(bounds);
				boundsF.Inflate(inflateBy, inflateBy);
				//path.AddRectangle(boundsF);
				path.AddArc(boundsF, 0, 360);
				return path;
			}
		}
	}
	#endregion // (Temporary) CompositeLinkDecorator test
	public partial class RolePlayerLink
	{
		#region MandatoryDotDecorator class
		/// <summary>
		/// The link decorator used to draw the mandatory
		/// constraint dot on a link.
		/// </summary>
		protected class MandatoryDotDecorator : LinkDecorator
		{
			/// <summary>
			/// Singleton instance of this decorator
			/// </summary>
			public static readonly LinkDecorator Decorator = new MandatoryDotDecorator();
			private MandatoryDotDecorator()
			{
				FillDecorator = true;
			}
			/// <summary>
			/// Return a circle slightly smaller than the standard decorator
			/// as the path
			/// </summary>
			/// <param name="bounds">A bounding rectangle for the decorator</param>
			/// <returns>A circle path</returns>
			protected override GraphicsPath GetPath(RectangleD bounds)
			{
				GraphicsPath path = new GraphicsPath();
				float inflateBy = -(float)(bounds.Width * .07);
				RectangleF boundsF = RectangleD.ToRectangleF(bounds);
				boundsF.Inflate(inflateBy, inflateBy);
				path.AddArc(boundsF, 0, 360);
				return path;
			}
		}
		#endregion // MandatoryDotDecorator class
		#region Customize appearance
		/// <summary>
		/// Draw the mandatory dot on the role box end, depending
		/// on the options settings
		/// </summary>
		public override LinkDecorator DecoratorFrom
		{
			get
			{
				if (OptionsPage.CurrentMandatoryDotPlacement != MandatoryDotPlacement.ObjectShapeEnd &&
					DrawMandatoryDot)
				{
					return MandatoryDotDecorator.Decorator;
				}
				return base.DecoratorFrom;
			}
			set
			{
			}
		}
		/// <summary>
		/// Draw the mandatory dot on the object type end, depending
		/// on the options settings
		/// </summary>
		public override LinkDecorator DecoratorTo
		{
			get
			{
				if (OptionsPage.CurrentMandatoryDotPlacement != MandatoryDotPlacement.RoleBoxEnd &&
					DrawMandatoryDot)
				{
					return MandatoryDotDecorator.Decorator;
				}
				return base.DecoratorTo;
			}
			set
			{
			}
		}
		/// <summary>
		/// Helper function to determine if we should draw a mandatory dot
		/// </summary>
		protected bool DrawMandatoryDot
		{
			get
			{
				bool retVal = false;
				ObjectTypePlaysRole link;
				Role role;
				if ((null != (link = AssociatedRolePlayerLink)) &&
					(null != (role = link.PlayedRoleCollection)) &&
					role.IsMandatory)
				{
					retVal = true;
				}
				return retVal;
			}
		}
		/// <summary>
		/// Change the outline pen to a thin black line for all instances
		/// of this shape.
		/// </summary>
		/// <param name="classStyleSet">The style set to modify</param>
		protected override void InitializeResources(StyleSet classStyleSet)
		{
			PenSettings penSettings = new PenSettings();
			penSettings.Width = 1.0F / 72.0F; // 1 Point. 0 Means 1 pixel, but should only be used for non-printed items
			penSettings.Alignment = PenAlignment.Center;
			classStyleSet.OverridePen(DiagramPens.ConnectionLine, penSettings);
		}
		/// <summary>
		/// Use a straight line routing style
		/// Use a center to center routing style
		/// </summary>
		[CLSCompliant(false)]
		protected override VGRoutingStyle DefaultRoutingStyle
		{
			get
			{
				return VGRoutingStyle.VGRouteCenterToCenter;
			}
		}
		/// <summary>
		/// Selecting role player links gets in the way of selecting roleboxes, etc.
		/// It is best just to turn them off. This also eliminates a bunch of unnamed
		/// roles from the property grid element picker.
		/// </summary>
		public override bool CanSelect
		{
			get
			{
				return false;
			}
		}
		#endregion // Customize appearance
		#region RolePlayerLink specific
		/// <summary>
		/// Stop the user from manually routine link lines
		/// </summary>
		/// <value>false</value>
		public override bool CanManuallyRoute
		{
			get
			{
				return false;
			}
		}
		/// <summary>
		/// Get the ObjectTypePlaysRole link associated with this link shape
		/// </summary>
		public ObjectTypePlaysRole AssociatedRolePlayerLink
		{
			get
			{
				return ModelElement as ObjectTypePlaysRole;
			}
		}
		#endregion // RolePlayerLink specific
		#region Shape display update rules
		private static void UpdateDotDisplayOnMandatoryConstraintChange(Role role)
		{
			foreach (ModelElement mel in role.GetElementLinks(ObjectTypePlaysRole.PlayedRoleCollectionMetaRoleGuid))
			{
				foreach (PresentationElement pel in mel.PresentationRolePlayers)
				{
					ShapeElement shape = pel as ShapeElement;
					if (shape != null)
					{
						shape.Invalidate(true);
					}
				}
			}
		}
		/// <summary>
		/// Update the link displays when a role sequence for a mandatory constraint is added
		/// </summary>
		[RuleOn(typeof(FactTypeHasInternalConstraint), FireTime = TimeToFire.TopLevelCommit)]
		private class InternalConstraintRoleSequenceAdded : AddRule
		{
			public override void ElementAdded(ElementAddedEventArgs e)
			{
				FactTypeHasInternalConstraint link = e.ModelElement as FactTypeHasInternalConstraint;
				SimpleMandatoryConstraint constraint = link.InternalConstraintCollection as SimpleMandatoryConstraint;
				if (constraint != null)
				{
					RoleMoveableCollection roles = constraint.RoleCollection;
					if (roles.Count > 0)
					{
						Debug.Assert(roles.Count == 1); // Mandatory constraints have a single role only
						UpdateDotDisplayOnMandatoryConstraintChange(roles[0]);
					}
				}
			}
		}
		/// <summary>
		/// Update the link display when a mandatory constraint role is removed
		/// </summary>
		[RuleOn(typeof(ConstraintRoleSequenceHasRole))]
		private class InternalConstraintRoleSequenceRoleRemoved : RemoveRule
		{
			public override void ElementRemoved(ElementRemovedEventArgs e)
			{
				ConstraintRoleSequenceHasRole link = e.ModelElement as ConstraintRoleSequenceHasRole;
				SimpleMandatoryConstraint constraint;
				Role role;
				if ((null != (constraint = link.ConstraintRoleSequenceCollection as SimpleMandatoryConstraint)) &&
				    (null != (role = link.RoleCollection)))
				{
					UpdateDotDisplayOnMandatoryConstraintChange(role);
				}
			}
		}
		#endregion // Shape display update rules
		#region Luminosity Modification
		/// <summary>
		/// Redirect all luminosity modification to the ORMDiagram.ModifyLuminosity
		/// algorithm
		/// </summary>
		/// <param name="currentLuminosity">The luminosity to modify</param>
		/// <param name="view">The view containing this item</param>
		/// <returns>Modified luminosity value</returns>
		protected override int ModifyLuminosity(int currentLuminosity, DiagramClientView view)
		{
			if (view.HighlightedShapes.Contains(new DiagramItem(this)))
			{
				return ORMDiagram.ModifyLuminosity(currentLuminosity);
			}
			return currentLuminosity;
		}
		#endregion // Luminosity Modification
	}
}
