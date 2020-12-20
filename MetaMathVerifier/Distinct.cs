namespace MetaMathVerifier
{
    public class Distinct : MMStatement
    {
        public string X;
        public string Y;
        public override bool Equals(object obj)
        {
            if (!(obj is Distinct)) return false;
            Distinct d = (Distinct)obj;
            return X == d.X && Y == d.Y;
        }
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ (7 * Y.GetHashCode());
        }
    }
}