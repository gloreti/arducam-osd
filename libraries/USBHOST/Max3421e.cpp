/* MAX3421E USB host controller support */

#include "Max3421e.h"
// #include "Max3421e_constants.h"

static byte vbusState;

    uint8_t _ss_pin;     //slave select
    uint8_t _int_pin;    //interrupt
    uint8_t _reset_pin;  //reset      

/* Functions    */

/* Constructor */
MAX3421E::MAX3421E( uint8_t _ss, uint8_t _int, uint8_t _reset )
{
    /* assign pins */
    _ss_pin = _ss;   
    _int_pin = _int;
    _reset_pin = _reset;
    
//    Serial.println( _ss_pin, DEC );
//    Serial.println( _int_pin, DEC );
//    Serial.println( _reset_pin, DEC );
    
    /* setup pins */
    pinMode( _int_pin, INPUT);
    pinMode( _ss_pin, OUTPUT );
    digitalWrite(_ss_pin,HIGH);   //deselect MAX3421E              
    pinMode( _reset_pin, OUTPUT );
    digitalWrite( _reset_pin, HIGH );  //release MAX3421E from reset
    
    
    //Serial.begin( 9600 );
    //init();
    //powerOn();
}
byte MAX3421E::getVbusState( void )
{ 
    return( vbusState );
}
byte MAX3421E::getvar( void )
{
  return( _int_pin );
}    
/* initialization */
void MAX3421E::init()
{
    /* setup pins */
//    pinMode( MAX_INT, INPUT);
//    pinMode( MAX_SS, OUTPUT );
//    Deselect_MAX3421E;              
//    pinMode( MAX_RESET, OUTPUT );
//    digitalWrite( MAX_RESET, HIGH );  //release MAX3421E from reset
}

/* toggles breakpoint pin during debug */
void MAX3421E::toggle( byte pin )
{
    digitalWrite( pin, HIGH );
    digitalWrite( pin, LOW );
}
/* Single host register write   */
void MAX3421E::regWr( byte reg, byte val)
{
  uint8_t SaveSREG = SREG;       //save interrupt flag
      cli();                        //disable interrupts
      digitalWrite(_ss_pin,LOW);
      Spi.transfer( reg + 2 ); //set WR bit and send register number
      Spi.transfer( val );
      digitalWrite(_ss_pin,HIGH);
      SREG = SaveSREG;              //restore interrupt flag 
}
/* multiple-byte write */
/* returns a pointer to a memory position after last written */
char* MAX3421E::bytesWr( byte reg, byte nbytes, char * data )
{
 uint8_t SaveSREG = SREG;         //save interrupt flag
  cli();                          //disable interrupts   
    digitalWrite(_ss_pin,LOW);    //assert SS
    Spi.transfer ( reg + 2 );   //set W/R bit and select register   
    while( nbytes ) {                
        Spi.transfer( *data );  // send the next data byte
        data++;                 // advance the pointer
        nbytes--;
    }
    digitalWrite(_ss_pin,HIGH);          //deassert SS
    SREG = SaveSREG;              //restore interrupt flag 
    return( data );
}
/* GPIO write. GPIO byte is split between 2 registers, so two writes are needed to write one byte */
/* GPOUT bits are in the low nibble. 0-3 in IOPINS1, 4-7 in IOPINS2 */
/* upper 4 bits of IOPINS1, IOPINS2 are read-only, so no masking is necessary */
void MAX3421E::gpioWr( byte val )
{
    regWr( rIOPINS1, val );
    val = val >>4;
    regWr( rIOPINS2, val );
    
    return;     
}
/* Single host register read        */
byte MAX3421E::regRd( byte reg )    
{
  byte tmp;
  uint8_t SaveSREG = SREG;       //save interrupt flag
    cli();                        //disable interrupts
    digitalWrite(_ss_pin,LOW);
    Spi.transfer ( reg );         //send register number
    tmp = Spi.transfer ( 0x00 );  //send empty byte, read register contents
    digitalWrite(_ss_pin,HIGH);
    SREG = SaveSREG;              //restore interrupt flag 
    return (tmp);
}
/* multiple-bytes register read                             */
/* returns a pointer to a memory position after last read   */
char * MAX3421E::bytesRd ( byte reg, byte nbytes, char  * data )
{
  uint8_t SaveSREG = SREG;       //save interrupt flag
    cli();                        //disable interrupts
    digitalWrite(_ss_pin,LOW);    //assert SS
    Spi.transfer ( reg );     //send register number
    while( nbytes ) {
        *data = Spi.transfer ( 0x00 );    //send empty byte, read register contents
        data++;
        nbytes--;
    }
    digitalWrite(_ss_pin,HIGH);  //deassert SS
    SREG = SaveSREG;              //restore interrupt flag
    return( data );   
}
/* GPIO read. See gpioWr for explanation */
/* GPIN pins are in high nibbles of IOPINS1, IOPINS2    */
byte MAX3421E::gpioRd( void )
{
 byte tmpbyte = 0;
    tmpbyte = regRd( rIOPINS2 );            //pins 4-7
    tmpbyte &= 0xf0;                        //clean lower nibble
    tmpbyte |= ( regRd( rIOPINS1 ) >>4 ) ;  //shift low bits and OR with upper from previous operation. Upper nibble zeroes during shift, at least with this compiler
    return( tmpbyte );
}
/* reset MAX3421E using chip reset bit. SPI configuration is not affected   */
boolean MAX3421E::reset()
{
  byte tmp = 0;
    regWr( rUSBCTL, bmCHIPRES );                        //Chip reset. This stops the oscillator
    regWr( rUSBCTL, 0x00 );                             //Remove the reset
    while(!(regRd( rUSBIRQ ) & bmOSCOKIRQ )) {          //wait until the PLL is stable
        tmp++;                                          //timeout after 256 attempts
        if( tmp == 0 ) {
            return( false );
        }
    }
    return( true );
}
/* turn USB power on/off                                                */
/* does nothing, returns TRUE. Left for compatibility with old sketches               */
/* will be deleted eventually                                           */
///* ON pin of VBUS switch (MAX4793 or similar) is connected to GPOUT7    */
///* OVERLOAD pin of Vbus switch is connected to GPIN7                    */
///* OVERLOAD state low. NO OVERLOAD or VBUS OFF state high.              */
boolean MAX3421E::vbusPwr ( boolean action )
{
//  byte tmp;
//    tmp = regRd( rIOPINS2 );                //copy of IOPINS2
//    if( action ) {                          //turn on by setting GPOUT7
//        tmp |= bmGPOUT7;
//    }
//    else {                                  //turn off by clearing GPOUT7
//        tmp &= ~bmGPOUT7;
//    }
//    regWr( rIOPINS2, tmp );                 //send GPOUT7
//    if( action ) {
//        delay( 60 );
//    }
//    if (( regRd( rIOPINS2 ) & bmGPIN7 ) == 0 ) {     // check if overload is present. MAX4793 /FLAG ( pin 4 ) goes low if overload
//        return( false );
//    }                      
    return( true );                                             // power on/off successful                       
}
/* probe bus to determine device presense and speed */
void MAX3421E::busprobe( void )
{
 byte bus_sample;
    bus_sample = regRd( rHRSL );            //Get J,K status
    bus_sample &= ( bmJSTATUS|bmKSTATUS );      //zero the rest of the byte
    switch( bus_sample ) {                          //start full-speed or low-speed host 
        case( bmJSTATUS ):
            if(( regRd( rMODE ) & bmLOWSPEED ) == 0 ) {
                regWr( rMODE, MODE_FS_HOST );       //start full-speed host
                vbusState = FSHOST;
            }
            else {
                regWr( rMODE, MODE_LS_HOST);        //start low-speed host
                vbusState = LSHOST;
            }
            break;
        case( bmKSTATUS ):
            if(( regRd( rMODE ) & bmLOWSPEED ) == 0 ) {
                regWr( rMODE, MODE_LS_HOST );       //start low-speed host
                vbusState = LSHOST;
            }
            else {
                regWr( rMODE, MODE_FS_HOST );       //start full-speed host
                vbusState = FSHOST;
            }
            break;
        case( bmSE1 ):              //illegal state
            vbusState = SE1;
            break;
        case( bmSE0 ):              //disconnected state
            vbusState = SE0;
            break;
        }//end switch( bus_sample )
}
/* MAX3421E initialization after power-on   */
void MAX3421E::powerOn()
{
    /* Configure full-duplex SPI, interrupt pulse   */
    regWr( rPINCTL,( bmFDUPSPI + bmINTLEVEL + bmGPXB ));    //Full-duplex SPI, level interrupt, GPX
    if( reset() == false ) {                                //stop/start the oscillator
        Serial.println("Error: OSCOKIRQ failed to assert");
    }
//    /* configure power switch   */
//    vbusPwr( OFF );                                         //turn Vbus power off
//    regWr( rGPINIEN, bmGPINIEN7 );                          //enable interrupt on GPIN7 (power switch overload flag)
//    if( vbusPwr( ON  ) == false ) {
//        Serial.println("Error: Vbus overload");
//    }
    /* configure host operation */
    regWr( rMODE, bmDPPULLDN|bmDMPULLDN|bmHOST|bmSEPIRQ );      // set pull-downs, Host, Separate GPIN IRQ on GPX
    regWr( rHIEN, bmCONDETIE/*|bmFRAMEIE */);                                             //connection detection
    regWr(rHCTL,bmSAMPLEBUS);                                               // update the JSTATUS and KSTATUS bits
    busprobe();                                                             //check if anything is connected
    regWr( rHIRQ, bmCONDETIRQ );                                            //clear connection detect interrupt                 
    //regWr( rHIRQ, 0xff );
    regWr( rCPUCTL, bmIE );                                                 //enable interrupt pin
}
/* MAX3421 state change task and interrupt handler */
byte MAX3421E::Task( void )
{
 byte rcode = 0;
 byte pinvalue;

    pinvalue = digitalRead( _int_pin );    
    if( pinvalue  == LOW ) {
        rcode = IntHandler();
    }
//    pinvalue = digitalRead( MAX_GPX );
//    if( pinvalue == LOW ) {
//        GpxHandler();
//    }
//    usbSM();                                //USB state machine                            
    return( rcode );   
}   
byte MAX3421E::IntHandler()
{
 byte HIRQ;
 byte HIRQ_sendback = 0x00;
    HIRQ = regRd( rHIRQ );                  //determine interrupt source
    if( HIRQ & bmFRAMEIRQ ) {               //->1ms SOF interrupt handler
        HIRQ_sendback |= bmFRAMEIRQ;
    }//end FRAMEIRQ handling
    if( HIRQ & bmCONDETIRQ ) {
        busprobe();
        HIRQ_sendback |= bmCONDETIRQ;
    }
    /* End HIRQ interrupts handling, clear serviced IRQs    */
    regWr( rHIRQ, HIRQ_sendback );
    return( HIRQ_sendback );
}
byte MAX3421E::GpxHandler()
{
 byte GPINIRQ = regRd( rGPINIRQ );          //read GPIN IRQ register
//    if( GPINIRQ & bmGPINIRQ7 ) {            //vbus overload
//        vbusPwr( OFF );                     //attempt powercycle
//        delay( 1000 );
//        vbusPwr( ON );
//        regWr( rGPINIRQ, bmGPINIRQ7 );
//    }       
    return( GPINIRQ );
}

//void MAX3421E::usbSM( void )                //USB state machine
//{
//    
//
//}