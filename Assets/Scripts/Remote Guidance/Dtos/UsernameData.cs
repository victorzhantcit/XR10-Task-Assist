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

        // �мg Equals ��k
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            UsernameData other = (UsernameData)obj;
            return username.Equals(other.username) && wsid.Equals(other.wsid);
        }

        // �мg GetHashCode ��k
        public override int GetHashCode()
        {
            // �X�� username �M wsid �����ƽX�A�קK���ƸI��
            return (username, wsid).GetHashCode();
        }
    }
}
