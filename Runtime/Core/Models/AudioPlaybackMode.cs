namespace CreativeArcana.Audio
{
    public enum AudioPlaybackMode
    {
        Single, //just AudioClip[0]
        Random,        
        RandomNoRepeat, //never play a clip twice in a row
        Sequential,  //Loop in order
        Shuffle //every clip plays once then shuffle, also lastClip != firstClip
    }
}