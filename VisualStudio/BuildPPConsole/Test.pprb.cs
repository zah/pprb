public class Modifiers
{

    

    
 // from foo 2

  /* long block
     of text

     with substitutions: */
   public void DynamicName() {}

    public LinearTransform str;
    public LinearTransform dex;
    public LinearTransform intelligence;
    public LinearTransform wisdom;

    public void Reset()
    {
        str = new LinearTransform();
        dex = new LinearTransform();
        intelligence = new LinearTransform();
        wisdom = new LinearTransform();
    }
}
