/* File Description
 * Original Works/Author: Thomas Slusny
 * Other Contributors: eXpl0it3r
 * Author Website: http://indiearmory.com
 * License: MIT
*/

using System;
using SFGL.Graphics;
using SFML.Graphics;
using SFML.Window;
using SFGL.Utils;

namespace SFGL.Extensions
{
	public static class SpriteExtensions
	{
		public static Rectangle GetAABB(this Sprite sp)
		{
			//Store the sprite position
			var pos = new Vector2(sp.Transform.TransformPoint(new Vector2f(0,0)));

			//Store the size so we can calculate the other corners
			var size = new Vector2(sp.TextureRect.Width, sp.TextureRect.Height);

			//Store the sprite current rotation
			var angle = sp.Rotation;

			//Bail out early if the sprite isn't rotated
			if (angle == 0.0f)
			{
				return new Rectangle(
					pos.X,
					pos.Y,
					size.X,
					size.Y);
			}

			//Calculate the other points as vectors from (0,0)
			//Imagine sf::Vector2f A(0,0); but its not necessary
			//as rotation is around this point.
			var B = new Vector2(sp.TextureRect.Width, 0);
			var C = new Vector2(sp.TextureRect.Width, sp.TextureRect.Height);
			var D = new Vector2(0, sp.TextureRect.Height);

			//Rotate the points to match the sprite rotation
			B.Rotate (angle);
			C.Rotate (angle);
			D.Rotate (angle);

			//Round off to int and set the four corners of our Rect
			var left = MinValue (0.0f, B.X, C.X, D.X);
			var top = MinValue(0.0f, B.Y, C.Y, D.Y);
			var right = MaxValue(0.0f, B.X, C.X, D.X);
			var bottom = MaxValue(0.0f, B.Y, C.Y, D.Y);

			//Create a Rect from out points and move it back to the correct position on the screen
			var AABB = new Rectangle(left, top, right - left, bottom - top);
			AABB.X += (int)pos.X;
			AABB.Y += (int)pos.Y;

			//AABB.Offset((int)(pos.X), (int)(pos.Y));
			return AABB;
		}

		public static bool PixelIntersects(this Sprite sp1, Sprite sp2, byte AlphaLimit)
		{
			//Get AABBs of the two sprites
			var Object1AABB = GetAABB(sp1);
			var Object2AABB = GetAABB(sp2);
			var Intersection = Rectangle.Zero;

			if (Object1AABB.Intersects(Object2AABB, out Intersection))
			{
				//We've got an intersection we need to process the pixels
				//In that Rect.

				//Bail out now if AlphaLimit = 0
				if (AlphaLimit == 0) return true;

				//There are a few hacks here, sometimes the TransformToLocal returns negative points
				//Or Points outside the image.  We need to check for these as they print to the error console
				//which is slow, and then return black which registers as a hit.

				var O1SubRect = sp1.TextureRect;
				var O2SubRect = sp2.TextureRect;

				//Vector2f O1SubRectSize = new Vector2f(sp1.Texture.Width, sp1.Texture.Height);
				//Vector2f O2SubRectSize = new Vector2f(sp2.Texture.Width, sp2.Texture.Height);
				var O1SubRectSize = new Vector2(O1SubRect.Width, O1SubRect.Height);
				var O2SubRectSize = new Vector2(O2SubRect.Width, O2SubRect.Height);

				var o1v = Vector2.Zero;
				var o2v = Vector2.Zero;

				var im1 = sp1.Texture.CopyToImage();
				var im2 = sp2.Texture.CopyToImage();

				//Loop through our pixels
				for (var i = Intersection.Left; i < Intersection.Left + Intersection.Width; i++)
				{
					for (var j = Intersection.Top; j < Intersection.Top + Intersection.Height; j++)
					{

						o1v = new Vector2(sp1.Transform.TransformPoint(new Vector2f(i, j))); //Creating Objects each loop :(
						o2v = new Vector2(sp2.Transform.TransformPoint(new Vector2f(i, j)));

						//Hack to make sure pixels fall within the Sprite's Image
						if (o1v.X > 0 && o1v.Y > 0 && o2v.X > 0 && o2v.Y > 0 &&
							o1v.X < O1SubRectSize.X && o1v.Y < O1SubRectSize.Y &&
							o2v.X < O2SubRectSize.X && o2v.Y < O2SubRectSize.Y)
						{

							//If both sprites have opaque pixels at the same point we've got a hit
							Color c1 = im1.GetPixel((uint)(o1v.X), (uint)(o1v.Y));
							Color c2 = im2.GetPixel((uint)(o2v.X), (uint)(o2v.Y));
							if ((c1.A > AlphaLimit) &&
								(c2.A > AlphaLimit))
							{
								return true;
							}
						}
					}
				}
				return false;
			}
			return false;
		}

		public static bool BoundingBoxTest(this Sprite sp1, Sprite sp2)
		{

			var A = Vector2.Zero;
			var B = Vector2.Zero;
			var C = Vector2.Zero;
			var BL= Vector2.Zero;
			var TR = Vector2.Zero;
			var HalfSize1 = new Vector2(sp1.TextureRect.Width, sp1.TextureRect.Height);
			var HalfSize2 = new Vector2(sp2.TextureRect.Width, sp2.TextureRect.Height);

			//For somereason the Vector2d divide by operator
			//was misbehaving
			//Doing it manually
			HalfSize1.X /= 2;
			HalfSize1.Y /= 2;
			HalfSize2.X /= 2;
			HalfSize2.Y /= 2;

			//Get the Angle we're working on
			var Angle = sp1.Rotation - sp2.Rotation;
			var CosA = (float)Math.Cos(FloatMath.ToRadians(Angle));
			var SinA = (float)Math.Sin(FloatMath.ToRadians(Angle));

			float t, x, a, dx, ext1, ext2;

			//Normalise the Center of Object2 so its axis aligned an represented in
			//relation to Object 1
			C = new Vector2(sp2.Position);

			C.X -= sp1.Position.X;
			C.Y -= sp1.Position.Y;

			C.Rotate (sp2.Rotation);

			//Get the Corners
			BL = TR = C;
			BL.X -= HalfSize2.X;
			BL.Y -= HalfSize2.Y;

			TR.X += HalfSize2.X;
			TR.Y += HalfSize2.Y;

			//Calculate the vertices of the rotate Rect
			A.X = -HalfSize1.Y * SinA;
			B.X = A.X;
			t = HalfSize1.X * CosA;
			A.X += t;
			B.X -= t;

			A.Y = HalfSize1.Y * CosA;
			B.Y = A.Y;
			t = HalfSize1.X * SinA;
			A.Y += t;
			B.Y -= t;

			t = SinA * CosA;

			// verify that A is vertical min/max, B is horizontal min/max
			if (t < 0)
			{
				t = A.X;
				A.X = B.X;
				B.X = t;
				t = A.Y;
				A.Y = B.Y;
				B.Y = t;
			}

			// verify that B is horizontal minimum (leftest-vertex)
			if (SinA < 0)
			{
				B.X = -B.X;
				B.Y = -B.Y;
			}

			// if rr2(ma) isn't in the horizontal range of
			// colliding with rr1(r), collision is impossible
			if (B.X > TR.X || B.X > -BL.X) return false;

			// if rr1(r) is axis-aligned, vertical min/max are easy to get
			if (t == 0)
			{
				ext1 = A.Y;
				ext2 = -ext1;
			}// else, find vertical min/max in the range [BL.x, TR.x]
			else
			{
				x = BL.X - A.X;
				a = TR.X - A.X;
				ext1 = A.Y;
				// if the first vertical min/max isn't in (BL.x, TR.x), then
				// find the vertical min/max on BL.x or on TR.x
				if (a * x > 0)
				{
					dx = A.X;
					if (x < 0)
					{
						dx -= B.X;
						ext1 -= B.Y;
						x = a;
					}
					else
					{
						dx += B.X;
						ext1 += B.Y;
					}
					ext1 *= x;
					ext1 /= dx;
					ext1 += A.Y;
				}

				x = BL.X + A.X;
				a = TR.X + A.X;
				ext2 = -A.Y;
				// if the second vertical min/max isn't in (BL.x, TR.x), then
				// find the local vertical min/max on BL.x or on TR.x
				if (a * x > 0)
				{
					dx = -A.X;
					if (x < 0)
					{
						dx -= B.X;
						ext2 -= B.Y;
						x = a;
					}
					else
					{
						dx += B.X;
						ext2 += B.Y;
					}
					ext2 *= x;
					ext2 /= dx;
					ext2 -= A.Y;
				}
			}

			// check whether rr2(ma) is in the vertical range of colliding with rr1(r)
			// (for the horizontal range of rr2)
			return !((ext1 < BL.Y && ext2 < BL.Y) ||
				(ext1 > TR.Y && ext2 > TR.Y));

		}

		private static float MinValue(float a, float b, float c, float d)
		{
			float min = a;

			min = (b < min ? b : min);
			min = (c < min ? c : min);
			min = (d < min ? d : min);

			return min;
		}

		private static float MaxValue(float a, float b, float c, float d)
		{
			float max = a;

			max = (b > max ? b : max);
			max = (c > max ? c : max);
			max = (d > max ? d : max);

			return max;
		}
	}
}

