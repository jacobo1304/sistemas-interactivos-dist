[System.Serializable]
public class CharacterResponse
{
    public CharacterData data;
}

[System.Serializable]
public class CharacterData
{
    public int mal_id;
    public string name;
    public CharacterImages images;
}

[System.Serializable]
public class CharacterImages
{
    public CharacterJpg jpg;
}

[System.Serializable]
public class CharacterJpg
{
    public string image_url;
}