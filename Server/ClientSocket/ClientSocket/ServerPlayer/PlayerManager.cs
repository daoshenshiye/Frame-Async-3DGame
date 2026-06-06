namespace ClientSocket.ServerPlayer;

public class PlayerManager
{
    private static PlayerManager instance;

    public static PlayerManager Instance
    {
        get
        {
            if (instance == null)
                instance = new PlayerManager();
            return instance;
        }
    }
    public Dictionary<int, Player> player_Dic = new Dictionary<int, Player>();

    public void AddPlayer(Player player)
    {
        if (!player_Dic.ContainsKey(player.id))
        {
            player_Dic.Add(player.id, player);
        }
    }

    public void DeletePlayer(int id)
    {
        if (player_Dic.ContainsKey(id))
        {
            player_Dic[id]=null;
            player_Dic.Remove(id);  
        }
    }
    public Player GetPlayer(int id)
    {
        return player_Dic.TryGetValue(id, out Player player) ? player : null;
    }
}