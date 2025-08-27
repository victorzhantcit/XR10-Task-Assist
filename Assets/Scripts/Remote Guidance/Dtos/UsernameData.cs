namespace Guidance.Dtos
{
    [System.Serializable]
    public class UsernameData
    {
        public string wsid;
        public string username;

        public UsernameData(string wsid, string username)
        {
            this.wsid = wsid;
            this.username = username;
        }

        // ÂÐ¼g Equals ¤èªk
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            UsernameData other = (UsernameData)obj;
            return username.Equals(other.username) && wsid.Equals(other.wsid);
        }

        // ÂÐ¼g GetHashCode ¤èªk
        public override int GetHashCode()
        {
            // ¦X¨Ö username ©M wsid ªº«¢§Æ½X¡AÁ×§K«¢§Æ¸I¼²
            return (username, wsid).GetHashCode();
        }
    }
}
