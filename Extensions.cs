public static class Extensions
{
    public static int BitCount(this long n)
    {
        var count = 0;
        while (n != 0)
        {
            count++;
            n &= n - 1; //walking through all the bits which are set to one
        }

        return count;
    }
}