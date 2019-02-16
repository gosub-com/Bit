All instructions are 32 bits, optionally followed by a 32 bit immediate value.

Registers 0 to 15 are general purpose 32 bit registers:
    R0..R13		General purpose
    R14 = S		Stack (by convention, since it's not special in any way)
    R15 = PC	Program counter
    
Registers 16 to 18 are extended registers:
    R16 = CC	Condition Codes
    R17 = RL	Link register (used for returning to subroutine)

Conditional Branches (branches are relative to PC)
0000CCCC ######## ######## ######## [optional immediate word]
    C = 00:BRN			01:BRA			02:BEQ			03:BNE
        04:BCS/BULT		05:BCC/BUGE		06:BVS			07:BVC
        08:BMI			09:BPL			0A:BULE			0B:BUGT  
        0C:BSLT			0D:BSGE			0E:BSLE			0F:BSGT
    # = 24 bit signed value times 4, or 0x800000 for immediate value follows
        
Jump to subroutine
0001CCCC ######## ######## ######## [optional immediate word]
    C = 10: JSR #		(Bump to subroutine, absolute)
        11: BSR #		(Branch to subroutine, relative to PC)
    # = 24 bit signed value times 4, or 0x800000 for immediate value follows
    NOTE: Alu instruction 0x56 is also a jump to subroutine (e.g LD.J PC,[R+S*4])

ALU integer op-code chart: 
        40:LD			41:ST			42:ADD			43:ADC
        44:SUB			45:SUBC			46:SUBR			47:SUBRC
        48:CMP			49:BIT			4A:AND			4B:OR
        4C:XOR			4D:SL			4E:SLC			4F:SR
        50:USR			51:SRC			52:MIN			53:UMIN
        54:MAX			55:UMAX			56:LD.J

ALU Instruction:
01CCCCCC	MMMMDDDD	[see below for next 16 bits]
    C: Alu operation (see ALU integer chart above)
    M: Mode (the next 16 bits is defined as follows)
        0: ########	########	D = D op #16u
        1: ########	########	D = D op #~16u
        2: SSSS####	########	D = S op #12
        3: SSSS#### TTTEx###	D = S op #7				[#32 follows when 0x40]
        4: SSSSRRRR	TTTExnnn	D = S op (R<<n)
        5: SSSSRRRR	TTTExiii	D = S op [R]			iii = [++R],[--R],[R++],[R--],[R]
        6: SSSSRRRR	TTTExnnn	D = D op [S+(R<<n)]
        7: SSSSRRRR	TTT#####	D = S op [R+#~5ux]		[#32 follows when 0]
        8: SSSSRRRR	TTT#####	D = S op [R+#5ux]
        9: SSSSRRRR	TTT#####	D = S op [R+#5ux+32x]
        A: SSSSRRRR	TTT#####	D = S op [R+#5ux+64x]
        B: SSSSRRRR	TTT#####	D = S op [R+#5ux+96x]
        C: ####RRRR	TTT#####	D = D op [R+#9ux]
        D: ####RRRR	TTT#####	D = D op [R+#9ux+512x]
        E: ####RRRR TTT#####	D = D op ->[R+#9ux]		[#32 follows when 0x1F]
        F: Reserved for future
    D: Destination register (and source register 1 for two register operations)
    S: Source register 1
    R: Source register 2
    n: Three bit number to shift a register left
    i: Pre/post inc/dec, i = (0:[++R], 1:[--R], 2:[R++], 3:[R--], 4:[R])
    x: Use extended registers (r0, r1, cc, link, ... s, pc)
    T: Type - sbyte,byte,short,ushort,int,(reserved for future: float,long,double)
        NOTE: The right hand operand is sign extended (or zero filled) before 
              the ALU operation.  The ALU operation is always performed as a 32 
              bit integer operation.  The ALU output can be sign extended (or
              zero filled) by setting the E bit.
    E: Sign extend (or zero fill if unsigned) the ALU result
    #16u = unsigned 16 bit number
    #~16u = unsigned 16 bit number | 0xFFFF0000 - always negative
    #12 (and #7) = signed 12 bit (or 7 bit) number
    #~5ux = (5 bit unsigned number | 0xFFFFFFe0) * sizeof(type) - always negative
    #5ux = 5 bit unsigned number * sizeof(type)
        NOTE: +32x, +64x, +96x = the number * sizeof(type)
    #9ux = 9 bit unsigned number * sizeof(type)
        


// **************************************************************************
uint32 Crc32(uint32 crc, uint32 *table, byte *buffer, int length)
{
    while(--length >= 0) 
        crc = table[((crc >> 24) ^ *buffer++)] ^ (crc << 8);	// normal
}

#define crc		r0
#define table	r1
#define buffer	r2
#define length	r3
    sub		length,1
    blt		crc_done
crc_loop:
    usr		r4,crc,24			// r4 = ((uint)crc >> 24)
    xor.b	r4,[buffer++]		// r4 = r4 ^ *buffer++
    sl		crc,8
    xor		crc,[table+r4*4]
    sub		length,1
    bge		crc_loop
crc_done:
    ld		pc,rlink			// Return to subroutine

// **************************************************************************
uint32 Crc32(uint32 crc, uint32 *table, byte *buffer, int length)
{
    while(length--) 
        crc = table[(crc ^ *buffer++) & 0xFFL] ^ (crc >> 8);	// reflected
}
#define crc		r0
#define table	r1
#define buffer	r2
#define length	r3
    sub		length,1
    blt		crc_done
crc_loop:
    xor.b.e	r4,crc,[buffer++]	// r4 = (crc ^ *buffer++) & 0xFF
    usr		crc,8
    xor		crc,[table+r4*4]
    sub		length,1
    bge		crc_loop
crc_done:
    ld		pc,rlink		// Return to subroutine


// **************************************************************************

    
    
    