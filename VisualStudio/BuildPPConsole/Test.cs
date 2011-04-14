public class Modifiers
{
    //> @modifiers = {}

    //> use 'Common'
    
    //> def modifier name, alt = name.downcase
    //>     @modifiers[name] = alt
    //> end

    //> modifier 'Strength', 'str'
    //> modifier 'Dexterity', 'dex'
    //> modifier 'Intelligence'
    //> modifier 'Wisdom'
    
    //> foo

    //% @modifiers.each do |k,v|
    public LinearTransform _v_;
    //-

    public void Reset()
    {
        //% @modifiers.each do |k,v|
        _v_ = new LinearTransform();
        //-
    }
}