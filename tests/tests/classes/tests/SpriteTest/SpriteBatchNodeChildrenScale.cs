using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cocos2d;

namespace tests
{
    public class SpriteBatchNodeChildrenScale : SpriteTestDemo
    {
        public SpriteBatchNodeChildrenScale()
        {
            CCSize s = CCDirector.SharedDirector.WinSize;

            CCSpriteFrameCache.SharedSpriteFrameCache.AddSpriteFramesWithFile("animations/grossini_family.plist");

            CCNode aParent;
            CCSprite sprite1, sprite2;
            CCActionInterval rot = CCRotateBy.Create(10, 360);
            CCAction seq = CCRepeatForever.Create(rot);

            //
            // Children + Scale using Sprite
            // Test 1
            //
            aParent = CCNode.Create();
            sprite1 = CCSprite.Create("grossinis_sister1.png");
            sprite1.Position = new CCPoint(s.width / 4, s.height / 4);
            sprite1.ScaleX = -0.5f;
            sprite1.ScaleY = 2.0f;
            sprite1.RunAction(seq);


            sprite2 = CCSprite.Create("grossinis_sister2.png");
            sprite2.Position = (new CCPoint(50, 0));

            AddChild(aParent);
            aParent.AddChild(sprite1);
            sprite1.AddChild(sprite2);


            //
            // Children + Scale using SpriteBatchNode
            // Test 2
            //

            aParent = CCSpriteBatchNode.Create("animations/grossini_family");
            sprite1 = CCSprite.Create("grossinis_sister1.png");
            sprite1.Position = new CCPoint(3 * s.width / 4, s.height / 4);
            sprite1.ScaleX = -0.5f;
            sprite1.ScaleY = 2.0f;
            sprite1.RunAction((CCAction)(seq.Copy()));

            sprite2 = CCSprite.Create("grossinis_sister2.png");
            sprite2.Position = (new CCPoint(50, 0));

            AddChild(aParent);
            aParent.AddChild(sprite1);
            sprite1.AddChild(sprite2);


            //
            // Children + Scale using Sprite
            // Test 3
            //

            aParent = CCNode.Create();
            sprite1 = CCSprite.Create("grossinis_sister1.png");
            sprite1.Position = (new CCPoint(s.width / 4, 2 * s.height / 3));
            sprite1.ScaleX = (1.5f);
            sprite1.ScaleY = -0.5f;
            sprite1.RunAction((CCAction)(seq.Copy()));

            sprite2 = CCSprite.Create("grossinis_sister2.png");
            sprite2.Position = (new CCPoint(50, 0));

            AddChild(aParent);
            aParent.AddChild(sprite1);
            sprite1.AddChild(sprite2);

            //
            // Children + Scale using Sprite
            // Test 4
            //

            aParent = CCSpriteBatchNode.Create("animations/grossini_family");
            sprite1 = CCSprite.Create("grossinis_sister1.png");
            sprite1.Position = (new CCPoint(3 * s.width / 4, 2 * s.height / 3));
            sprite1.ScaleX = 1.5f;
            sprite1.ScaleY = -0.5f;
            sprite1.RunAction((CCAction)(seq.Copy()));

            sprite2 = CCSprite.Create("grossinis_sister2.png");
            sprite2.Position = (new CCPoint(50, 0));

            AddChild(aParent);
            aParent.AddChild(sprite1);
            sprite1.AddChild(sprite2);
        }

        public override string title()
        {
            return "Sprite/BatchNode + child + scale + rot";
        }
    }
}