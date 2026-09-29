namespace Smash
{
    //AI keys: the buttons are "pressed" by an AiBrain instead of a device
    class BotKeys : BaseKeys
    {
        readonly AiBrain brain;

        public BotKeys(AiBrain brain)
        {
            this.brain = brain;
        }

        public override string Name => "CPU LV" + brain.Level;

        protected override InputButtons Poll() => brain.Think();
    }
}
