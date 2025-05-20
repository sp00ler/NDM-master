namespace NetDeviceManager.Models
{
    public class IpNetmask
    {
        public int IntMask { get; }
        public IpNetmask(string mask)
        {
            try
            {
                int count = 0;
                foreach (string part in mask.Split("."))
                {
                    string str = part.Replace(".", string.Empty);
                    int temp;
                    temp = int.Parse(part);
                    count += Convert.ToString(temp, 2).Count(c => c == '1');
                }
                IntMask = count;
            }
            catch { IntMask = 0; }
           
        }

        
    }
}
