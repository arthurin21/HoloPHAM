using System;
using UnityEngine;

public class DisplayMessage : MonoBehaviour
{		
	public enum FontType : byte
	{
	   COURIER,
	   TIMES_ROMAN,
	   ARIAL
	}

	public enum FontStyleType : byte
	{
		// This enumeration's values corresponds to bit positions.  If a fourth
	    // type is added, its value will be 4; a fifth type will be 8, etc.
   		NORMAL = 0,
   		BOLD = 1,
   		ITALICS = 2
	}
	
	public string m_id;
	
	protected string _mesg;
	protected Int16 _x;
	protected Int16 _y;
	protected UInt16 _width;
	protected UInt16 _height;
	protected bool _drawBorder;
	protected FontType _fontType;
	protected FontStyleType _fontStyleType;
	protected byte _fontSize;
	protected Color _color;	
	
	protected GUIContent guicontent;
	protected GUIStyle guistyle;
	protected Rect guiwindow;
	
	protected bool hasContent;
	
	public void AssignValues(string msgId, string mesg, short x, short y, ushort width, ushort height, bool drawBorder, 
		FontType fontType, FontStyleType fontStyleType, byte fontSize, Color color)
	{
		m_id = msgId;
		_mesg = mesg;
		_x = x;
		_y = y;
		_width = width;
		_height = height;
		_drawBorder = drawBorder;
		_fontType = fontType;
		_fontStyleType = fontStyleType;
		_fontSize = fontSize;
		_color = color;	// should be converted prior to this function
		
		hasContent = false;
	}
		
	public void AssignDefaultValues()
	{		
		m_id = "HelloWorldMessage";
		_mesg = "Hello World";
		_x = 10;
		_y = 20;
		_width = 100;
		_height = 30;
		_drawBorder = true;
		_fontType = FontType.TIMES_ROMAN;
		_fontStyleType = FontStyleType.NORMAL;
		_fontSize = 10;
		_color = new Color(0,0,0,1);
		
		hasContent = false;
	}
	
	void OnGUI()
	{
		if( !hasContent )
		{
			guicontent = new GUIContent(_mesg);
			if( _drawBorder )
				guistyle = new GUIStyle(GUI.skin.button);
			else
				guistyle = new GUIStyle(GUI.skin.label);
			switch( _fontType )
			{					
				case FontType.COURIER:
					guistyle.font = (Font)Resources.Load("Fonts/cour",typeof(Font));
					break;
				case FontType.TIMES_ROMAN:
					guistyle.font = (Font)Resources.Load("Fonts/times",typeof(Font));
					break;
				case FontType.ARIAL:
					guistyle.font = (Font)Resources.Load("Fonts/arial",typeof(Font));
					break;
				default:
					Debug.LogWarning("Invalid Font Type");
					break;
			}
			if( _fontStyleType == FontStyleType.NORMAL )
				guistyle.fontStyle = FontStyle.Normal;
			else if( _fontStyleType == FontStyleType.BOLD )
				guistyle.fontStyle = FontStyle.Bold;
			else if( _fontStyleType == FontStyleType.ITALICS )
				guistyle.fontStyle = FontStyle.Italic;
			else if( _fontStyleType == (FontStyleType.BOLD | FontStyleType.ITALICS) )
				guistyle.fontStyle = FontStyle.BoldAndItalic;		
			guistyle.fontSize = Convert.ToInt32(_fontSize);
			guistyle.normal.textColor = _color;								
			guiwindow = new Rect(Convert.ToSingle(_x), Convert.ToSingle(_y), 
				Convert.ToSingle(_width), Convert.ToSingle(_height));	
			
			hasContent = true;
		}
		
		if( _drawBorder )
			GUI.Box(guiwindow, guicontent, guistyle);			
		else			
			GUI.Label(guiwindow, guicontent.text, guistyle);	// TODO this still has a box around the text
	}
}


